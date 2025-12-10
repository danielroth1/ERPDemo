using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DashboardAnalytics.Configuration;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Services;
using DashboardAnalytics.HealthChecks;
using DashboardAnalytics.Hubs;
using DashboardAnalytics.GraphQL;
using Prometheus;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Configure settings
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection("MongoDb"));
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<KafkaSettings>(
        builder.Configuration.GetSection("Kafka"));

    // Add infrastructure
    builder.Services.AddSingleton<MongoDbContext>();
    builder.Services.AddHostedService<KafkaConsumerService>();

    // Add services
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<IKPIService, KPIService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IDatabaseOverviewService, DatabaseOverviewService>();
    builder.Services.AddSingleton<IPublishDatabaseUpdateService, PublishDatabaseUpdateService>();

    // Add memory cache and distributed cache
    builder.Services.AddMemoryCache();
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
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
        .AddQueryType(d => d.Name("Query"))
            .AddTypeExtension<Query>()
            .AddTypeExtension<DatabaseQuery>()
        .AddMutationType(d => d.Name("Mutation"))
            .AddTypeExtension<Mutation>()
            .AddTypeExtension<DatabaseMutation>()
        .AddSubscriptionType(d => d.Name("Subscription"))
            .AddTypeExtension<Subscription>()
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
        .AddCheck<MongoDbHealthCheck>("mongodb");

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
    
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapMetrics();

    // Display startup information
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5005";
    var environment = app.Environment.EnvironmentName;
    var mongoDbSettings = app.Configuration.GetSection("MongoDb").Get<MongoDbSettings>();
    
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
    Console.WriteLine($"Database:           MongoDB - {mongoDbSettings?.ConnectionString.Split('@').LastOrDefault()?.Split('?').FirstOrDefault() ?? "configured"}");
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
