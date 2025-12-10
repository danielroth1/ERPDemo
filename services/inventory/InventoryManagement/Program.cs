using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Prometheus;
using InventoryManagement.Configuration;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add configuration settings
var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>()
    ?? throw new InvalidOperationException("MongoDB settings not configured");
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()
    ?? throw new InvalidOperationException("Kafka settings not configured");

// Register settings as singletons
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(kafkaSettings);

// Register infrastructure
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<KafkaProducer>();

// Register services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddSingleton<IFinancialAccountInitializer, FinancialAccountInitializer>();

// Add HttpClient for service-to-service communication
builder.Services.AddHttpClient<IFinancialServiceClient, FinancialServiceClient>();

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
    .AddCheck<InventoryManagement.HealthChecks.MongoDbHealthCheck>(
        "mongodb",
        timeout: TimeSpan.FromSeconds(3));

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready");

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
    Console.WriteLine($"Database:           MongoDB - {mongoSettings.ConnectionString.Split('@').LastOrDefault()?.Split('?').FirstOrDefault() ?? "configured"}");
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
