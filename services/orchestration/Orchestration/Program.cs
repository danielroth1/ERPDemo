using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;
using Prometheus;
using Orchestration.Configuration;
using Orchestration.Consumers;
using Orchestration.Infrastructure;
using Orchestration.Sagas;
using Orchestration.Services;
using ERP.Contracts;
using ERP.Contracts.Events;
using ERP.Contracts.Events.Domain;
using ERP.Contracts.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console(new CompactJsonFormatter());
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            cfg.WriteTo.OpenTelemetry();
    });

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? ""))
            };
        });
    builder.Services.AddAuthorization();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3001",
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:5175",
                    "https://shopping-now.net"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Controllers
    builder.Services.AddControllers();

    // Trackers (saga-to-HTTP bridge)
    builder.Services.AddSingleton<PurchaseTracker>();
    builder.Services.AddSingleton<ReturnTracker>();

    // PostgreSQL for saga state persistence
    var pgConnection = builder.Configuration.GetConnectionString("erp-orchestration")
        ?? builder.Configuration["PostgreSQL:ConnectionString"]
        ?? "Host=localhost;Port=5432;Database=erp_orchestration;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<OrchestrationDbContext>(options =>
        options.UseNpgsql(pgConnection)
            .UseSnakeCaseNamingConvention());

    // RabbitMQ + Kafka configuration
    // Aspire injects ConnectionStrings__rabbitmq as amqp://user:pass@host:port
    // Fall back to individual settings when running standalone (non-Aspire)
    var rabbitmqUri = builder.Configuration.GetConnectionString("rabbitmq");
    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    var kafkaBootstrap = builder.Configuration.GetConnectionString("kafka")
        ?? builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    builder.Services.AddMassTransit(x =>
    {
        // Saga state machines with EF Core persistence
        x.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<OrchestrationDbContext>();
                r.UsePostgres();
            });
        x.AddSagaStateMachine<ReturnStateMachine, ReturnState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<OrchestrationDbContext>();
                r.UsePostgres();
            });

        // EF Core outbox — persists saga Send/Publish calls atomically with saga state transitions.
        // UseBusOutbox() is intentionally omitted: the controller has no DB operation to be atomic with,
        // so SubmitPurchase publishes directly to RabbitMQ. Only the consumer-level outbox is needed here.
        x.AddEntityFrameworkOutbox<OrchestrationDbContext>(o =>
        {
            o.UsePostgres();
            o.QueryDelay = TimeSpan.FromMilliseconds(50);
        });

        // Apply outbox filter to all receive endpoints
        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<OrchestrationDbContext>(context);
        });

        // Result consumers
        x.AddConsumer<PurchaseCompletedConsumer>();
        x.AddConsumer<PurchaseFailedConsumer>();
        x.AddConsumer<ReturnCompletedConsumer>();
        x.AddConsumer<ReturnFailedConsumer>();

        // RabbitMQ transport — unlocks outbox, retry, dead-letter
        x.UsingRabbitMq((context, cfg) =>
        {
            if (!string.IsNullOrEmpty(rabbitmqUri))
                cfg.Host(new Uri(rabbitmqUri));
            else
                cfg.Host(rabbitHost, "/", h => { h.Username(rabbitUser); h.Password(rabbitPass); });

            cfg.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)));

            // Performance filters — log message age, consume/publish/send duration
            cfg.UseConsumeFilter(typeof(PerfConsumeFilter<>), context);
            cfg.UsePublishFilter(typeof(PerfPublishFilter<>), context);
            cfg.UseSendFilter(typeof(PerfSendFilter<>), context);

            cfg.ConfigureEndpoints(context);
        });

        // Kafka Rider — publish domain events (fan-out to dashboard, etc.)
        x.AddRider(rider =>
        {
            // Domain event producers (Kafka stays for fan-out)
            rider.AddProducer<PurchaseCompleted>(KafkaTopics.PurchaseCompletedEvent);
            rider.AddProducer<PurchaseFailed>(KafkaTopics.PurchaseFailedEvent);
            rider.AddProducer<ReturnCompleted>(KafkaTopics.ReturnCompletedEvent);
            rider.AddProducer<ReturnFailed>(KafkaTopics.ReturnFailedEvent);

            rider.UsingKafka((_, k) =>
            {
                k.Host(kafkaBootstrap);
            });
        });
    });

    // Health checks
    builder.Services.AddHealthChecks();

    // Swagger / OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Background service for stuck saga detection
    builder.Services.AddHostedService<SagaTimeoutService>();

    var app = builder.Build();

    // Ensure saga database schema — Migrate() applies new tables (like outbox) to existing databases
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orchestration API v1"));
    }
    app.UseHttpMetrics();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapDefaultEndpoints();
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        // Exclude MassTransit checks (auto-tagged "ready" in v8) — Kafka connectivity must not block readiness
        Predicate = check => check.Tags.Contains("ready") && !check.Name.StartsWith("masstransit", StringComparison.OrdinalIgnoreCase)
    });
    app.MapMetrics();

    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5010";
    var environment = app.Environment.EnvironmentName;

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("ORCHESTRATION SERVICE - Saga Workflow Coordinator");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Environment:        {environment}");
    Console.WriteLine($"Listening on:       {urls}");
    Console.WriteLine($"Health Check:       {urls}/health/live");
    Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine(new string('=', 80) + "\n");

    Log.Information("Orchestration service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
