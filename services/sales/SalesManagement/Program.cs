using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;
using SalesManagement.Configuration;
using SalesManagement.GraphQL;
using SalesManagement.HealthChecks;
using SalesManagement.Infrastructure;
using SalesManagement.Services;

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
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

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

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales Management API v1");
    });
}

app.UseSerilogRequestLogging();

app.UseRouting();

// Prometheus metrics
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

// Health check endpoint
app.MapHealthChecks("/health");

// Metrics endpoint
app.MapMetrics("/metrics");

// Display startup information
var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5003";
var environment = app.Environment.EnvironmentName;

Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("SALES MANAGEMENT SERVICE - Enterprise Resource Planning System");
Console.WriteLine(new string('=', 80));
Console.WriteLine($"Service Name:       Sales Management");
Console.WriteLine($"Version:            1.0.0");
Console.WriteLine($"Environment:        {environment}");
Console.WriteLine($"Listening on:       {urls}");
Console.WriteLine($"Swagger UI:         {urls}/swagger");
Console.WriteLine($"GraphQL:            {urls}/graphql");
Console.WriteLine($"Health Check:       {urls}/health");
Console.WriteLine($"Metrics:            {urls}/metrics");
Console.WriteLine($"Database:           MongoDB - {mongoDbSettings.ConnectionString.Split('@').LastOrDefault()?.Split('?').FirstOrDefault() ?? "configured"}");
Console.WriteLine($"Started at:         {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine(new string('=', 80) + "\n");

Log.Information("Sales Management Service started successfully");

app.Run();
