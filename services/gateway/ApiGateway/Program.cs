using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiGateway.Configuration;
using ApiGateway.Consumers;
using ApiGateway.Sagas;
using ApiGateway.Services;
using AspNetCoreRateLimit;
using Confluent.Kafka;
using ERP.Contracts;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

// Bootstrap logger (before config is loaded)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Aspire service defaults (service discovery, OpenTelemetry, health checks)
    builder.AddServiceDefaults();

    // Replace bootstrap logger with full config-driven logger (picks up GrafanaLoki sink from appsettings)
    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console(new CompactJsonFormatter());
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            cfg.WriteTo.OpenTelemetry();
    });

    // Configure settings
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<ServiceEndpoints>(
        builder.Configuration.GetSection("Services"));

    // Add JWT Authentication
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

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("authenticated", policy =>
        {
            policy.RequireAuthenticatedUser();
        });
    });

    // Add Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Add CORS
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

    // Add YARP Reverse Proxy
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // Add Controllers (for ShopController — saga-based purchase/return)
    builder.Services.AddControllers();

    // Add PurchaseTracker / ReturnTracker (saga-to-HTTP bridge)
    builder.Services.AddSingleton<PurchaseTracker>();
    builder.Services.AddSingleton<ReturnTracker>();

    // Configure MassTransit with Kafka Rider
    var kafkaBootstrap = builder.Configuration.GetConnectionString("kafka")
        ?? builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    builder.Services.AddMassTransit(x =>
    {
        // Register saga state machines with in-memory repository
        x.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
            .InMemoryRepository();
        x.AddSagaStateMachine<ReturnStateMachine, ReturnState>()
            .InMemoryRepository();

        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });

        x.AddRider(rider =>
        {
            // Register consumers
            rider.AddConsumer<PurchaseCompletedConsumer>();
            rider.AddConsumer<PurchaseFailedConsumer>();
            rider.AddConsumer<ReturnCompletedConsumer>();
            rider.AddConsumer<ReturnFailedConsumer>();

            // Register saga state machines for Kafka
            rider.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>();
            rider.AddSagaStateMachine<ReturnStateMachine, ReturnState>();

            // Register producers
            rider.AddProducer<SubmitPurchase>(KafkaTopics.SubmitPurchase);
            rider.AddProducer<SubmitReturn>(KafkaTopics.SubmitReturn);
            rider.AddProducer<ReserveStock>(KafkaTopics.ReserveStockCommand);
            rider.AddProducer<DeductStock>(KafkaTopics.DeductStockCommand);
            rider.AddProducer<RestoreStock>(KafkaTopics.RestoreStockCommand);
            rider.AddProducer<CreatePurchaseTransaction>(KafkaTopics.CreatePurchaseTransactionCommand);
            rider.AddProducer<CreateRefundTransaction>(KafkaTopics.CreateRefundTransactionCommand);
            rider.AddProducer<PurchaseCompleted>(KafkaTopics.PurchaseCompletedEvent);
            rider.AddProducer<PurchaseFailed>(KafkaTopics.PurchaseFailedEvent);
            rider.AddProducer<ReturnCompleted>(KafkaTopics.ReturnCompletedEvent);
            rider.AddProducer<ReturnFailed>(KafkaTopics.ReturnFailedEvent);

            rider.UsingKafka((context, k) =>
            {
                k.Host(kafkaBootstrap);

                // Saga consumes SubmitPurchase
                k.TopicEndpoint<SubmitPurchase>(KafkaTopics.SubmitPurchase, "gateway-purchase-saga", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                // Saga consumes SubmitReturn
                k.TopicEndpoint<SubmitReturn>(KafkaTopics.SubmitReturn, "gateway-return-saga", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<ReturnState>(context);
                });

                // Saga consumes stock/transaction events
                k.TopicEndpoint<StockReserved>(KafkaTopics.StockReservedEvent, "gateway-purchase-saga-events", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                k.TopicEndpoint<StockReservationFailed>(KafkaTopics.StockReservationFailedEvent, "gateway-purchase-saga-failures", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                k.TopicEndpoint<PurchaseTransactionCreated>(KafkaTopics.PurchaseTransactionCreatedEvent, "gateway-purchase-saga-txn", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                k.TopicEndpoint<PurchaseTransactionFailed>(KafkaTopics.PurchaseTransactionFailedEvent, "gateway-purchase-saga-txn-fail", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                k.TopicEndpoint<StockDeducted>(KafkaTopics.StockDeductedEvent, "gateway-purchase-saga-deducted", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                k.TopicEndpoint<StockDeductionFailed>(KafkaTopics.StockDeductionFailedEvent, "gateway-purchase-saga-deduct-fail", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<PurchaseState>(context);
                });

                // Return saga events
                k.TopicEndpoint<RefundTransactionCreated>(KafkaTopics.RefundTransactionCreatedEvent, "gateway-return-saga-refund", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<ReturnState>(context);
                });

                k.TopicEndpoint<RefundTransactionFailed>(KafkaTopics.RefundTransactionFailedEvent, "gateway-return-saga-refund-fail", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<ReturnState>(context);
                });

                k.TopicEndpoint<StockRestored>(KafkaTopics.StockRestoredEvent, "gateway-return-saga-restored", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureSaga<ReturnState>(context);
                });

                // Result consumers (bridge back to HTTP)
                k.TopicEndpoint<PurchaseCompleted>(KafkaTopics.PurchaseCompletedEvent, "gateway-purchase-results", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<PurchaseCompletedConsumer>(context);
                });

                k.TopicEndpoint<PurchaseFailed>(KafkaTopics.PurchaseFailedEvent, "gateway-purchase-failures", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<PurchaseFailedConsumer>(context);
                });

                k.TopicEndpoint<ReturnCompleted>(KafkaTopics.ReturnCompletedEvent, "gateway-return-results", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<ReturnCompletedConsumer>(context);
                });

                k.TopicEndpoint<ReturnFailed>(KafkaTopics.ReturnFailedEvent, "gateway-return-failures", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<ReturnFailedConsumer>(context);
                });
            });
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure middleware
    app.UseSerilogRequestLogging();

    // Enable WebSocket support
    app.UseWebSockets();

    app.UseCors();

    // Rate limiting
    app.UseIpRateLimiting();

    // Prometheus metrics
    app.UseHttpMetrics();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers (ShopController for saga-based purchase/return)
    app.MapControllers();

    // Map YARP routes
    app.MapReverseProxy();

    // Aspire default endpoints
    app.MapDefaultEndpoints();

    // Health checks
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapMetrics();

    // Display startup information
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
    var environment = app.Environment.EnvironmentName;

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("API GATEWAY - Enterprise Resource Planning System");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Service Name:       API Gateway");
    Console.WriteLine($"Version:            1.0.0");
    Console.WriteLine($"Environment:        {environment}");
    Console.WriteLine($"Listening on:       {urls}");
    Console.WriteLine($"Health Check:       {urls}/health/live");
    Console.WriteLine($"Ready Check:        {urls}/health/ready");
    Console.WriteLine($"Metrics:            {urls}/metrics");
    Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine(new string('=', 80) + "\n");

    Log.Information("API Gateway started successfully");
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
