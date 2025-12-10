using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;
using FinancialManagement.Configuration;
using FinancialManagement.HealthChecks;
using FinancialManagement.Infrastructure;
using FinancialManagement.Services;
using FinancialManagement.Models.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Load configuration
var mongoDbSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>()
    ?? throw new InvalidOperationException("MongoDb configuration is missing");
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is missing");
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()
    ?? throw new InvalidOperationException("Kafka configuration is missing");

// Add services to the container
builder.Services.AddSingleton(mongoDbSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(kafkaSettings);

// Register infrastructure services
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<KafkaProducer>();

// Register application services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

// Add Controllers
builder.Services.AddControllers();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

// Add Prometheus metrics
builder.Services.UseHttpClientMetrics();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseRouting();

// Prometheus metrics
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Metrics endpoint
app.MapMetrics("/metrics");

// Initialize default accounts
await InitializeDefaultAccountsAsync(app.Services);

// Display startup information
var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5004";
var environment = app.Environment.EnvironmentName;

Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("FINANCIAL MANAGEMENT SERVICE - Enterprise Resource Planning System");
Console.WriteLine(new string('=', 80));
Console.WriteLine($"Service Name:       Financial Management");
Console.WriteLine($"Version:            1.0.0");
Console.WriteLine($"Environment:        {environment}");
Console.WriteLine($"Listening on:       {urls}");
Console.WriteLine($"Swagger UI:         {urls}/swagger");
Console.WriteLine($"Health Check:       {urls}/health");
Console.WriteLine($"Metrics:            {urls}/metrics");
Console.WriteLine($"Database:           MongoDB - {mongoDbSettings.ConnectionString.Split('@').LastOrDefault()?.Split('?').FirstOrDefault() ?? "configured"}");
Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine(new string('=', 80) + "\n");

Log.Information("Financial Management Service started successfully");

app.Run();

// Helper method to initialize default accounts
static async Task InitializeDefaultAccountsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var revenueAccountNumber = configuration["DefaultAccounts:RevenueAccountNumber"] ?? "4000-REVENUE";
        var revenueAccountName = configuration["DefaultAccounts:RevenueAccountName"] ?? "Product Sales Revenue";

        // Check if revenue account already exists
        var existingAccount = await accountService.GetAccountByNumberAsync(revenueAccountNumber);
        if (existingAccount == null)
        {
            // Create default revenue account
            var revenueAccount = await accountService.CreateAccountAsync(new CreateAccountRequest
            {
                Name = revenueAccountName,
                Type = "Revenue",
                Category = "CurrentAssets",
                Currency = "USD",
                Description = "Revenue from product sales"
            });

            logger.LogInformation("Created default revenue account: {AccountNumber} - {AccountName} (ID: {AccountId})",
                revenueAccount.AccountNumber, revenueAccount.Name, revenueAccount.Id);

            Console.WriteLine($"\n✅ Default revenue account created successfully!");
            Console.WriteLine($"   Account Number: {revenueAccount.AccountNumber}");
            Console.WriteLine($"   Account ID:     {revenueAccount.Id}");
            Console.WriteLine($"   Account Name:   {revenueAccount.Name}\n");
        }
        else
        {
            logger.LogInformation("Default revenue account already exists: {AccountNumber} (ID: {AccountId})",
                existingAccount.AccountNumber, existingAccount.Id);
            
            Console.WriteLine($"\n✅ Default revenue account exists:");
            Console.WriteLine($"   Account Number: {existingAccount.AccountNumber}");
            Console.WriteLine($"   Account ID:     {existingAccount.Id}");
            Console.WriteLine($"   Account Name:   {existingAccount.Name}\n");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize default accounts");
        Console.WriteLine($"\n❌ Failed to create default revenue account: {ex.Message}\n");
    }
}
