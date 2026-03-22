using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;
using Confluent.Kafka;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using FinancialManagement.Configuration;
using FinancialManagement.Consumers;
using FinancialManagement.Infrastructure;
using FinancialManagement.Services;
using FinancialManagement.Models.DTOs;
using ERP.Contracts;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using ERP.Contracts.Events.Domain;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (service discovery, OpenTelemetry, health checks)
builder.AddServiceDefaults();

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(new CompactJsonFormatter());
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
    loggerConfig = loggerConfig.WriteTo.OpenTelemetry();
Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Load configuration
var postgresConnectionString = builder.Configuration.GetConnectionString("erp-financial")
    ?? builder.Configuration["PostgreSQL:ConnectionString"]
    ?? throw new InvalidOperationException("PostgreSQL not configured");
var postgresSettings = builder.Configuration.GetSection("PostgreSQL").Get<PostgresSettings>() ?? new PostgresSettings();
postgresSettings.ConnectionString = postgresConnectionString;
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is missing");
// Add services to the container
builder.Services.AddSingleton(postgresSettings);
builder.Services.AddSingleton(jwtSettings);

// Register PostgreSQL DbContext with dynamic JSON support
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(postgresConnectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource)
        .UseSnakeCaseNamingConvention());

// Register application services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Configure MassTransit with Kafka Rider
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
        rider.AddConsumer<CreatePurchaseTransactionConsumer>();
        rider.AddConsumer<CreateRefundTransactionConsumer>();
        rider.AddConsumer<UserCreatedConsumer>();

        // Producers for saga events + domain events
        rider.AddProducer<PurchaseTransactionCreated>(KafkaTopics.PurchaseTransactionCreatedEvent);
        rider.AddProducer<PurchaseTransactionFailed>(KafkaTopics.PurchaseTransactionFailedEvent);
        rider.AddProducer<RefundTransactionCreated>(KafkaTopics.RefundTransactionCreatedEvent);
        rider.AddProducer<RefundTransactionFailed>(KafkaTopics.RefundTransactionFailedEvent);
        rider.AddProducer<TransactionCreated>(KafkaTopics.TransactionCreatedEvent);
        rider.AddProducer<BudgetExceeded>(KafkaTopics.BudgetExceededEvent);

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafkaBootstrap);

            k.TopicEndpoint<CreatePurchaseTransaction>(KafkaTopics.CreatePurchaseTransactionCommand, "financial-purchase-tx", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<CreatePurchaseTransactionConsumer>(context);
            });

            k.TopicEndpoint<CreateRefundTransaction>(KafkaTopics.CreateRefundTransactionCommand, "financial-refund-tx", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<CreateRefundTransactionConsumer>(context);
            });

            k.TopicEndpoint<UserCreated>(KafkaTopics.UserCreatedEvent, "financial-user-created", e =>
            {
                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                e.ConfigureConsumer<UserCreatedConsumer>(context);
            });
        });
    });
});

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
    .AddNpgSql(
        postgresSettings.ConnectionString,
        name: "postgresql",
        timeout: TimeSpan.FromSeconds(3));

// Add Prometheus metrics
builder.Services.UseHttpClientMetrics();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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

// Aspire default endpoints
app.MapDefaultEndpoints();

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
Console.WriteLine($"Database:           PostgreSQL - {postgresSettings.ConnectionString.Split(';').FirstOrDefault(s => s.Contains("Host="))?.Replace("Host=", "") ?? "configured"}");
Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine(new string('=', 80) + "\n");

Log.Information("Financial Management Service started successfully");

app.Run();

// Helper method to initialize default accounts
static async Task InitializeDefaultAccountsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var systemAccounts = new[]
    {
        new { Name = "Company Operating Account", Type = "Asset", Category = "CurrentAssets", Description = "Main company operating account" },
        new { Name = "Sales Tax Payable", Type = "Liability", Category = "CurrentLiabilities", Description = "Sales tax collected from customers" },
        new { Name = "Product Inventory", Type = "Asset", Category = "Inventory", Description = "Value of products in stock" },
        new { Name = "Cost of Goods Sold", Type = "Expense", Category = "CostOfGoodsSold", Description = "Cost of products sold to customers" },
        new { Name = "Product Sales Revenue", Type = "Revenue", Category = "OperatingRevenue", Description = "Revenue from product sales" }
    };

    Console.WriteLine("\n" + new string('-', 80));
    Console.WriteLine("Initializing System Accounts");
    Console.WriteLine(new string('-', 80));

    try
    {
        foreach (var account in systemAccounts)
        {
            var existingAccount = await accountService.GetAccountByNameAsync(account.Name);

            if (existingAccount == null)
            {
                var createdAccount = await accountService.CreateAccountAsync(new CreateAccountRequest
                {
                    Name = account.Name,
                    Type = account.Type,
                    Category = account.Category,
                    Currency = "USD",
                    Description = account.Description
                });

                logger.LogInformation("Created system account: {AccountNumber} - {AccountName} (ID: {AccountId})",
                    createdAccount.AccountNumber, createdAccount.Name, createdAccount.Id);

                Console.WriteLine($"✅ Created: {account.Name}");
                Console.WriteLine($"   Number: {createdAccount.AccountNumber} | ID: {createdAccount.Id}");
            }
            else
            {
                logger.LogInformation("System account already exists: {AccountNumber} - {AccountName} (ID: {AccountId})",
                    existingAccount.AccountNumber, existingAccount.Name, existingAccount.Id);

                Console.WriteLine($"✓  Exists: {account.Name}");
                Console.WriteLine($"   Number: {existingAccount.AccountNumber} | ID: {existingAccount.Id}");
            }
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine("System Accounts Initialization Complete");
        Console.WriteLine(new string('-', 80) + "\n");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize system accounts");
        Console.WriteLine($"\n❌ Failed to initialize system accounts: {ex.Message}\n");
    }
}
