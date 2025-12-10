using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiGateway.Configuration;
using AspNetCoreRateLimit;
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
    });    // Add Rate Limiting
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
                    "http://localhost:3000",
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

    // Add Health Checks
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

    // Map YARP routes
    app.MapReverseProxy();

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
