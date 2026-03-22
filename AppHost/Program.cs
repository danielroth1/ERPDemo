var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var erpUsers = postgres.AddDatabase("erp-users", "erp_users");
var erpInventory = postgres.AddDatabase("erp-inventory", "erp_inventory");
var erpSales = postgres.AddDatabase("erp-sales", "erp_sales");
var erpFinancial = postgres.AddDatabase("erp-financial", "erp_financial");
var erpDashboard = postgres.AddDatabase("erp-dashboard", "erp_dashboard");

var kafka = builder.AddKafka("kafka")
    .WithDataVolume();

var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Services (order matters — declare dependencies before dependents)
var userManagement = builder.AddProject<Projects.UserManagement>("user-management")
    .WithReference(erpUsers)
    .WithReference(kafka);

var financial = builder.AddProject<Projects.FinancialManagement>("financial")
    .WithReference(erpFinancial)
    .WithReference(kafka);

var inventory = builder.AddProject<Projects.InventoryManagement>("inventory")
    .WithReference(erpInventory)
    .WithReference(kafka)
    .WithReference(financial);

var sales = builder.AddProject<Projects.SalesManagement>("sales")
    .WithReference(erpSales)
    .WithReference(kafka);

var dashboard = builder.AddProject<Projects.DashboardAnalytics>("dashboard")
    .WithReference(erpDashboard)
    .WithReference(kafka)
    .WithReference(redis);

// Gateway — needs references to all backend services for YARP routing
var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(kafka)
    .WithEnvironment(context =>
    {
        // Override Services:* config (used by ServiceEndpoints binding)
        context.EnvironmentVariables["Services__UserManagement"] = userManagement.GetEndpoint("http");
        context.EnvironmentVariables["Services__Inventory"] = inventory.GetEndpoint("http");
        context.EnvironmentVariables["Services__Sales"] = sales.GetEndpoint("http");
        context.EnvironmentVariables["Services__Financial"] = financial.GetEndpoint("http");
        context.EnvironmentVariables["Services__Dashboard"] = dashboard.GetEndpoint("http");

        // Override YARP cluster destination addresses
        context.EnvironmentVariables["ReverseProxy__Clusters__user-cluster__Destinations__destination1__Address"] = userManagement.GetEndpoint("http");
        context.EnvironmentVariables["ReverseProxy__Clusters__inventory-cluster__Destinations__destination1__Address"] = inventory.GetEndpoint("http");
        context.EnvironmentVariables["ReverseProxy__Clusters__sales-cluster__Destinations__destination1__Address"] = sales.GetEndpoint("http");
        context.EnvironmentVariables["ReverseProxy__Clusters__financial-cluster__Destinations__destination1__Address"] = financial.GetEndpoint("http");
        context.EnvironmentVariables["ReverseProxy__Clusters__dashboard-cluster__Destinations__destination1__Address"] = dashboard.GetEndpoint("http");
    });

// Frontend (Vite dev server)
builder.AddViteApp("frontend", "../frontend", "dev")
    .WithReference(gateway)
    .WithHttpEndpoint(port: 5173, targetPort: 5173);

builder.Build().Run();
