using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
var postgresSettings = builder.Configuration.GetSection("PostgreSQL").Get<PostgresSettings>()
    ?? throw new InvalidOperationException("PostgreSQL settings not configured");
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()
    ?? throw new InvalidOperationException("Kafka settings not configured");

// Register settings as singletons
builder.Services.AddSingleton(postgresSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(kafkaSettings);

// Register PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresSettings.ConnectionString)
        .UseSnakeCaseNamingConvention());

// Register Kafka producer
builder.Services.AddSingleton<KafkaProducer>();

// Register services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddScoped<IFinancialAccountInitializer, FinancialAccountInitializer>();

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
        timeout: TimeSpan.FromSeconds(3));

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
