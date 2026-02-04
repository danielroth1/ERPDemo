using System.CommandLine;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace ApiClientGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var serviceOption = new Option<string>(
            name: "--service",
            description: "Specific service to generate clients for (inventory, sales, dashboard, all)",
            getDefaultValue: () => "all");

        var checkOption = new Option<bool>(
            name: "--check",
            description: "Check if services are running without generating clients",
            getDefaultValue: () => false);

        var rootCommand = new RootCommand("Generate Kiota API clients for inter-service communication")
        {
            serviceOption,
            checkOption
        };

        rootCommand.SetHandler(async (service, check) =>
        {
            await GenerateClients(service, check);
        }, serviceOption, checkOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateClients(string service, bool checkOnly)
    {
        WriteColor("🚀 Kiota API Client Generator - Backend Services", ConsoleColor.Cyan);
        WriteColor("==================================================", ConsoleColor.Cyan);
        Console.WriteLine();

        var services = new Dictionary<string, ServiceInfo>
        {
            ["user-management"] = new("http://localhost:5001/swagger/v1/swagger.json", "User Management"),
            ["inventory"] = new("http://localhost:5002/swagger/v1/swagger.json", "Inventory"),
            ["sales"] = new("http://localhost:5003/swagger/v1/swagger.json", "Sales"),
            ["financial"] = new("http://localhost:5004/swagger/v1/swagger.json", "Financial"),
            ["dashboard"] = new("http://localhost:5005/swagger/v1/swagger.json", "Dashboard")
        };

        // Check services if requested
        if (checkOnly)
        {
            WriteColor("🔍 Checking all services...", ConsoleColor.Yellow);
            Console.WriteLine();

            bool allRunning = true;
            foreach (var (key, info) in services)
            {
                bool running = await TestServiceRunning(info.Url, info.DisplayName);
                if (!running) allRunning = false;
            }

            Console.WriteLine();
            if (allRunning)
            {
                WriteColor("✅ All services are running!", ConsoleColor.Green);
                Environment.Exit(0);
            }
            else
            {
                WriteColor("⚠️  Some services are not running. Start them before generating clients.", ConsoleColor.Yellow);
                Environment.Exit(1);
            }
        }

        // Define client generation configurations
        var clientConfigs = new List<ClientConfig>();

        if (service == "all" || service == "inventory")
        {
            clientConfigs.Add(new ClientConfig
            {
                ServiceName = "Inventory Service",
                Dependencies = new[] { "Financial" },
                Clients = new[]
                {
                    new ClientGeneration(
                        "http://localhost:5004/swagger/v1/swagger.json",
                        "./services/inventory/InventoryManagement/Generated/Clients/Financial",
                        "FinancialServiceClient",
                        "InventoryManagement.Generated.Clients.Financial"
                    )
                }
            });
        }

        if (service == "all" || service == "sales")
        {
            clientConfigs.Add(new ClientConfig
            {
                ServiceName = "Sales Service",
                Dependencies = new[] { "Inventory", "Financial" },
                Clients = new[]
                {
                    new ClientGeneration(
                        "http://localhost:5002/swagger/v1/swagger.json",
                        "./services/sales/SalesManagement/Generated/Clients/Inventory",
                        "InventoryServiceClient",
                        "SalesManagement.Generated.Clients.Inventory"
                    ),
                    new ClientGeneration(
                        "http://localhost:5004/swagger/v1/swagger.json",
                        "./services/sales/SalesManagement/Generated/Clients/Financial",
                        "FinancialServiceClient",
                        "SalesManagement.Generated.Clients.Financial"
                    )
                }
            });
        }

        if (service == "all" || service == "dashboard")
        {
            clientConfigs.Add(new ClientConfig
            {
                ServiceName = "Dashboard Service",
                Dependencies = new[] { "User Management", "Inventory", "Sales", "Financial" },
                Clients = new[]
                {
                    new ClientGeneration(
                        "http://localhost:5001/swagger/v1/swagger.json",
                        "./services/dashboard/DashboardAnalytics/Generated/Clients/UserManagement",
                        "UserManagementServiceClient",
                        "DashboardAnalytics.Generated.Clients.UserManagement"
                    ),
                    new ClientGeneration(
                        "http://localhost:5002/swagger/v1/swagger.json",
                        "./services/dashboard/DashboardAnalytics/Generated/Clients/Inventory",
                        "InventoryServiceClient",
                        "DashboardAnalytics.Generated.Clients.Inventory"
                    ),
                    new ClientGeneration(
                        "http://localhost:5003/swagger/v1/swagger.json",
                        "./services/dashboard/DashboardAnalytics/Generated/Clients/Sales",
                        "SalesServiceClient",
                        "DashboardAnalytics.Generated.Clients.Sales"
                    ),
                    new ClientGeneration(
                        "http://localhost:5004/swagger/v1/swagger.json",
                        "./services/dashboard/DashboardAnalytics/Generated/Clients/Financial",
                        "FinancialServiceClient",
                        "DashboardAnalytics.Generated.Clients.Financial"
                    )
                }
            });
        }

        if (clientConfigs.Count == 0)
        {
            WriteColor($"❌ Unknown service: {service}", ConsoleColor.Red);
            WriteColor("Available services: inventory, sales, dashboard, all", ConsoleColor.Yellow);
            Environment.Exit(1);
        }

        WriteColor($"📦 Generating clients for: {service}", ConsoleColor.Yellow);
        Console.WriteLine();

        int successCount = 0;
        int failCount = 0;
        int skipCount = 0;

        foreach (var config in clientConfigs)
        {
            WriteColor("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            WriteColor($"📦 {config.ServiceName}", ConsoleColor.Cyan);
            WriteColor("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            WriteColor($"Dependencies: {string.Join(", ", config.Dependencies)}", ConsoleColor.DarkGray);
            Console.WriteLine();

            foreach (var client in config.Clients)
            {
                var result = await GenerateClient(client);
                if (result == GenerationResult.Success) successCount++;
                else if (result == GenerationResult.Failed) failCount++;
                else skipCount++;
            }
        }

        // Summary
        Console.WriteLine();
        WriteColor("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
        WriteColor("📊 Final Summary", ConsoleColor.Cyan);
        WriteColor("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
        WriteColor($"  ✅ Successful: {successCount}", ConsoleColor.Green);
        WriteColor($"  ❌ Failed: {failCount}", ConsoleColor.Red);
        WriteColor($"  ⚠️  Skipped: {skipCount}", ConsoleColor.Yellow);
        Console.WriteLine();

        if (failCount == 0 && skipCount == 0)
        {
            WriteColor("✅ All backend API clients generated successfully!", ConsoleColor.Green);
            WriteColor("✅ Services can now use Kiota-generated clients for inter-service communication", ConsoleColor.Green);
            Environment.Exit(0);
        }
        else if (skipCount > 0)
        {
            WriteColor("⚠️  Some services were not running. Start them and regenerate.", ConsoleColor.Yellow);
            Environment.Exit(0);
        }
        else
        {
            WriteColor("⚠️  Some services failed to generate clients", ConsoleColor.Yellow);
            WriteColor("Make sure all required services are running and try again", ConsoleColor.Yellow);
            Environment.Exit(1);
        }
    }

    static async Task<bool> TestServiceRunning(string url, string name)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await client.GetAsync(url); // Changed from HEAD to GET
            if (response.IsSuccessStatusCode)
            {
                WriteColor($"  ✅ {name} - Running", ConsoleColor.Green);
                return true;
            }
        }
        catch
        {
            // Ignore
        }

        WriteColor($"  ❌ {name} - Not running", ConsoleColor.Red);
        return false;
    }

    static async Task<GenerationResult> GenerateClient(ClientGeneration client)
    {
        Console.WriteLine($"Generating client from {client.OpenApiUrl}...");

        // Check if service is running
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await httpClient.GetAsync(client.OpenApiUrl); // Changed from HEAD to GET
            if (!response.IsSuccessStatusCode)
            {
                WriteColor($"  ⚠️  Service not available: {client.OpenApiUrl}", ConsoleColor.Yellow);
                return GenerationResult.Skipped;
            }
        }
        catch (Exception ex)
        {
            WriteColor($"  ⚠️  Service not available: {ex.Message}", ConsoleColor.Yellow);
            return GenerationResult.Skipped;
        }

        WriteColor("  ✅ Service is running", ConsoleColor.Green);

        // Create output directory
        var outputDir = Path.GetFullPath(client.OutputPath);
        Directory.CreateDirectory(outputDir);

        // Generate client
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"kiota generate " +
                       $"--language CSharp " +
                       $"--openapi \"{client.OpenApiUrl}\" " +
                       $"--output \"{client.OutputPath}\" " +
                       $"--class-name {client.ClassName} " +
                       $"--namespace-name {client.Namespace} " +
                       $"--clean-output " +
                       $"--backing-store " +
                       $"--additional-data",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            WriteColor("  ❌ Failed to start kiota process", ConsoleColor.Red);
            return GenerationResult.Failed;
        }

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            WriteColor($"  ✅ Generated {client.ClassName}", ConsoleColor.Green);
            return GenerationResult.Success;
        }
        else
        {
            var error = await process.StandardError.ReadToEndAsync();
            WriteColor($"  ❌ Failed to generate {client.ClassName}", ConsoleColor.Red);
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($"  Error: {error}");
            return GenerationResult.Failed;
        }
    }

    static void WriteColor(string message, ConsoleColor color)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = oldColor;
    }

    record ServiceInfo(string Url, string DisplayName);
    record ClientGeneration(string OpenApiUrl, string OutputPath, string ClassName, string Namespace);
    record ClientConfig
    {
        public required string ServiceName { get; init; }
        public required string[] Dependencies { get; init; }
        public required ClientGeneration[] Clients { get; init; }
    }
    enum GenerationResult { Success, Failed, Skipped }
}
