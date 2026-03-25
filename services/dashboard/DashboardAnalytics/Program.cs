using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Confluent.Kafka;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using DashboardAnalytics.Configuration;
using DashboardAnalytics.Consumers;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Services;
using DashboardAnalytics.Hubs;
using DashboardAnalytics.GraphQL;
using Query = DashboardAnalytics.GraphQL.Query;
using Mutation = DashboardAnalytics.GraphQL.Mutation;
using Subscription = DashboardAnalytics.GraphQL.Subscription;
using Prometheus;
using ERP.Contracts;
using ERP.Contracts.Events.Domain;

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter());
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
    loggerConfig = loggerConfig.WriteTo.OpenTelemetry();
Log.Logger = loggerConfig.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Aspire service defaults (service discovery, OpenTelemetry, health checks)
    builder.AddServiceDefaults();

    // Add Serilog
    builder.Host.UseSerilog();

    // Configure settings
    var postgresConnectionString = builder.Configuration.GetConnectionString("erp-dashboard")
        ?? builder.Configuration["PostgreSQL:ConnectionString"]
        ?? throw new InvalidOperationException("PostgreSQL not configured");
    var postgresSettings = builder.Configuration.GetSection("PostgreSQL").Get<PostgresSettings>() ?? new PostgresSettings();
    postgresSettings.ConnectionString = postgresConnectionString;
    builder.Services.AddSingleton(postgresSettings);
    
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("Jwt"));

    // Add PostgreSQL DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(postgresConnectionString)
            .UseSnakeCaseNamingConvention());

    // Configure MassTransit with Kafka Rider (replaces raw KafkaConsumerService)
    var kafkaBootstrap = builder.Configuration.GetConnectionString("kafka")
        ?? builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    builder.Services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });

        x.AddRider(rider =>
        {
            // User event consumers
            rider.AddConsumer<UserCreatedConsumer>();
            rider.AddConsumer<UserUpdatedConsumer>();

            // Inventory event consumers
            rider.AddConsumer<ProductCreatedConsumer>();
            rider.AddConsumer<ProductUpdatedConsumer>();
            rider.AddConsumer<LowStockAlertConsumer>();

            // Sales event consumers
            rider.AddConsumer<OrderCreatedConsumer>();
            rider.AddConsumer<OrderStatusChangedConsumer>();

            // Financial event consumers
            rider.AddConsumer<TransactionCreatedConsumer>();
            rider.AddConsumer<BudgetExceededConsumer>();

            rider.UsingKafka((context, k) =>
            {
                k.Host(kafkaBootstrap);

                k.TopicEndpoint<UserCreated>(KafkaTopics.UserCreatedEvent, "dashboard-user-created", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                });

                k.TopicEndpoint<UserUpdated>(KafkaTopics.UserUpdatedEvent, "dashboard-user-updated", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<UserUpdatedConsumer>(context);
                });

                k.TopicEndpoint<ProductCreated>(KafkaTopics.ProductCreatedEvent, "dashboard-product-created", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<ProductCreatedConsumer>(context);
                });

                k.TopicEndpoint<ProductUpdated>(KafkaTopics.ProductUpdatedEvent, "dashboard-product-updated", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                });

                k.TopicEndpoint<LowStockAlert>(KafkaTopics.LowStockAlertEvent, "dashboard-low-stock", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<LowStockAlertConsumer>(context);
                });

                k.TopicEndpoint<OrderCreated>(KafkaTopics.OrderCreatedEvent, "dashboard-order-created", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<OrderCreatedConsumer>(context);
                });

                k.TopicEndpoint<OrderStatusChanged>(KafkaTopics.OrderStatusChangedEvent, "dashboard-order-status", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<OrderStatusChangedConsumer>(context);
                });

                k.TopicEndpoint<TransactionCreated>(KafkaTopics.TransactionCreatedEvent, "dashboard-transaction", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<TransactionCreatedConsumer>(context);
                });

                k.TopicEndpoint<BudgetExceeded>(KafkaTopics.BudgetExceededEvent, "dashboard-budget", e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<BudgetExceededConsumer>(context);
                });
            });
        });
    });

    // Add services
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<IKPIService, KPIService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IDatabaseOverviewService, DatabaseOverviewService>();
    builder.Services.AddSingleton<IPublishDatabaseUpdateService, PublishDatabaseUpdateService>();

    // Add memory cache and distributed cache
    builder.Services.AddMemoryCache();
    var redisConnectionString = builder.Configuration.GetConnectionString("redis")
        ?? builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "DashboardCache:";
        
        // Configure connection options for better resilience
        options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        options.ConfigurationOptions.ConnectTimeout = 10000; // 10 seconds
        options.ConfigurationOptions.SyncTimeout = 10000;
        options.ConfigurationOptions.ConnectRetry = 3;
        options.ConfigurationOptions.ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(5000);
        options.ConfigurationOptions.AbortOnConnectFail = false; // Important for startup
    });

    // Add SignalR
    builder.Services.AddSignalR();

    // Add GraphQL
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>()
            .AddTypeExtension<DatabaseQuery>()
        .AddMutationType<Mutation>()
            .AddTypeExtension<DatabaseMutation>()
        .AddSubscriptionType<Subscription>()
            .AddTypeExtension<DatabaseSubscription>()
        .AddInMemorySubscriptions();

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

            // For SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/dashboardHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Add controllers
    builder.Services.AddControllers();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            postgresSettings.ConnectionString,
            name: "postgresql",
            tags: new[] { "ready" },
            timeout: TimeSpan.FromSeconds(3));

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("*");
        });
    });

    var app = builder.Build();

    // Apply migrations on startup in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }

    // Configure middleware
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseRouting();
    
    // Enable WebSocket support for SignalR
    app.UseWebSockets();

    // Prometheus metrics
    app.UseHttpMetrics();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGraphQL();
    app.MapHub<DashboardHub>("/dashboardHub");

    // Aspire default endpoints
    app.MapDefaultEndpoints();
    
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapMetrics();

    // Display startup information
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5005";
    var environment = app.Environment.EnvironmentName;
    
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("DASHBOARD & ANALYTICS SERVICE - Enterprise Resource Planning System");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Service Name:       Dashboard & Analytics");
    Console.WriteLine($"Version:            1.0.0");
    Console.WriteLine($"Environment:        {environment}");
    Console.WriteLine($"Listening on:       {urls}");
    Console.WriteLine($"Swagger UI:         {urls}/swagger");
    Console.WriteLine($"GraphQL:            {urls}/graphql");
    Console.WriteLine($"SignalR Hub:        {urls}/dashboardHub");
    Console.WriteLine($"Health Check:       {urls}/health/live");
    Console.WriteLine($"Ready Check:        {urls}/health/ready");
    Console.WriteLine($"Metrics:            {urls}/metrics");
    Console.WriteLine($"Database:           PostgreSQL - {postgresSettings.ConnectionString.Split(';').FirstOrDefault(s => s.Contains("Host="))?.Replace("Host=", "") ?? "configured"}");
    Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine(new string('=', 80) + "\n");
    
    Log.Information("Dashboard & Analytics service started successfully");
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
