using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Prometheus;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using InventoryManagement.Configuration;
using InventoryManagement.Consumers;
using InventoryManagement.GraphQL;
using InventoryManagement.GraphQL.Types;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;
using ERP.Contracts;
using ERP.Contracts.Events.Domain;
using ERP.Contracts.Infrastructure;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (service discovery, OpenTelemetry, health checks)
builder.AddServiceDefaults();

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration);
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
    loggerConfig = loggerConfig.WriteTo.OpenTelemetry();
Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add configuration settings
var postgresConnectionString = builder.Configuration.GetConnectionString("erp-inventory")
    ?? builder.Configuration["PostgreSQL:ConnectionString"]
    ?? throw new InvalidOperationException("PostgreSQL not configured");
var postgresSettings = builder.Configuration.GetSection("PostgreSQL").Get<PostgresSettings>() ?? new PostgresSettings();
postgresSettings.ConnectionString = postgresConnectionString;
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");

// Register settings as singletons
builder.Services.AddSingleton(postgresSettings);
builder.Services.AddSingleton(jwtSettings);

// Register MinIO object storage
var minioSettings = builder.Configuration.GetSection("Minio").Get<MinioSettings>() ?? new MinioSettings();
builder.Services.AddSingleton(minioSettings);
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(minioSettings.Endpoint)
    .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
    .WithSSL(minioSettings.UseSSL)
    .Build());
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

// Register PostgreSQL DbContext (scoped — used by controllers and services)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresSettings.ConnectionString)
        .UseSnakeCaseNamingConvention());

// Register DbContext factory for GraphQL DataLoaders (scoped factory to match DbContext lifetime)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(postgresSettings.ConnectionString)
        .UseSnakeCaseNamingConvention(), ServiceLifetime.Scoped);

// Register GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<ProductTypeExtension>()
    .AddTypeExtension<ProductGqlConfiguration>()
    .AddTypeExtension<ProductDocumentGqlConfiguration>()
    .AddProjections()
    .AddFiltering();

// Register services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddScoped<IFinancialAccountInitializer, FinancialAccountInitializer>();

// Configure MassTransit with RabbitMQ for saga commands, Kafka Rider for domain events
var kafkaBootstrap = builder.Configuration.GetConnectionString("kafka")
    ?? builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
// Aspire injects ConnectionStrings__rabbitmq as amqp://user:pass@host:port
var rabbitmqUri = builder.Configuration.GetConnectionString("rabbitmq");
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // EF Core outbox — persists messages atomically with business data, releases DB lock before broker publish
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromMilliseconds(50);
    });

    // Apply outbox filter to all receive endpoints
    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<AppDbContext>(context);
    });

    // Register consumers for saga commands (RabbitMQ)
    x.AddConsumer<ReserveStockConsumer>();
    x.AddConsumer<DeductStockConsumer>();
    x.AddConsumer<RestoreStockConsumer>();
    x.AddConsumer<ReleaseReservationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrEmpty(rabbitmqUri))
            cfg.Host(new Uri(rabbitmqUri));
        else
            cfg.Host(rabbitHost, "/", h => { h.Username("guest"); h.Password("guest"); });

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

    // Kafka Rider for domain event producers only
    x.AddRider(rider =>
    {
        rider.AddProducer<ProductCreated>(KafkaTopics.ProductCreatedEvent);
        rider.AddProducer<ProductUpdated>(KafkaTopics.ProductUpdatedEvent);
        rider.AddProducer<ProductDeleted>(KafkaTopics.ProductDeletedEvent);
        rider.AddProducer<StockUpdated>(KafkaTopics.StockUpdatedEvent);
        rider.AddProducer<LowStockAlert>(KafkaTopics.LowStockAlertEvent);
        rider.AddProducer<StockMovementCreated>(KafkaTopics.StockMovementCreatedEvent);

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafkaBootstrap);
        });
    });
});

// Add HttpClient factory for inter-service communication
builder.Services.AddHttpClient("FinancialService", client =>
{
    var baseUrl = builder.Configuration["Services:Financial"] ?? "http://financial:8080";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Register Kiota-based Financial Service client
builder.Services.AddScoped<IFinancialServiceClient, FinancialServiceClientWrapper>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        postgresSettings.ConnectionString,
        name: "postgresql",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(3));

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations on startup (all environments including K8s)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Ensure MinIO buckets exist (dev only; in prod use infra-as-code)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
    try { await fileStorage.EnsureBucketsExistAsync(); }
    catch (Exception ex)
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(ex, "MinIO bucket setup failed — object storage may not be ready yet");
    }
}

// Configure middleware
app.UseSerilogRequestLogging();

app.UseCors();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Management API v1"));
}

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// GraphQL endpoint — authentication enforced here and at the gateway level
app.MapGraphQL().RequireAuthorization();

// Aspire default endpoints
app.MapDefaultEndpoints();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Root endpoint removed - use /health/ready or API endpoints under /api/v1/*

try
{
    // Display startup information
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5002";
    var environment = app.Environment.EnvironmentName;

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("INVENTORY MANAGEMENT SERVICE - Enterprise Resource Planning System");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Service Name:       Inventory Management");
    Console.WriteLine($"Version:            1.0.0");
    Console.WriteLine($"Environment:        {environment}");
    Console.WriteLine($"Listening on:       {urls}");
    Console.WriteLine($"Swagger UI:         {urls}/swagger");
    Console.WriteLine($"Health Check:       {urls}/health/live");
    Console.WriteLine($"Ready Check:        {urls}/health/ready");
    Console.WriteLine($"Metrics:            {urls}/metrics");
    Console.WriteLine($"Database:           PostgreSQL - {postgresSettings.ConnectionString.Split(';').FirstOrDefault(s => s.Contains("Host="))?.Replace("Host=", "") ?? "configured"}");
    Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine(new string('=', 80) + "\n");

    Log.Information("Inventory Management Service started successfully");
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

// Make the implicit Program class accessible to integration test projects
public partial class Program { }
