---
applyTo: "**/*.IntegrationTests/**"
---

# Integration Tests

All integration tests live co-located with their service under `services/<name>/<Name>.IntegrationTests/`.
Shared infrastructure is in `tests/shared/ERP.Testing.Shared/`.

## Project layout

```
services/<name>/
└── <Name>.IntegrationTests/
    ├── <Name>.IntegrationTests.csproj
    ├── GlobalUsings.cs
    ├── Collections/
    │   └── IntegrationTestCollection.cs   ← xUnit ICollectionFixture wiring
    ├── Fixtures/
    │   └── <Name>IntegrationFixture.cs    ← container + WebApplicationFactory
    └── Controllers/
        └── <Controller>Tests.cs
```

## Packages (copy from any existing .csproj)

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" ... />
<PackageReference Include="FluentAssertions" Version="6.12.1" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="coverlet.collector" Version="6.0.2" ... />
```

```xml
<ProjectReference Include="..\<ServiceName>\<ServiceName>.csproj" />
<ProjectReference Include="..\..\..\tests\shared\ERP.Testing.Shared\ERP.Testing.Shared.csproj" />
```

Also add to the root `erp.sln`:
```
dotnet sln add services/<name>/<Name>.IntegrationTests/<Name>.IntegrationTests.csproj
```

## Service-side prerequisites

**Program.cs** — add at the very end so `WebApplicationFactory<Program>` can see the type:
```csharp
public partial class Program { }
```

**<Service>.csproj** — expose internals to test projects:
```xml
<ItemGroup>
  <InternalsVisibleTo Include="<Name>.IntegrationTests" />
  <InternalsVisibleTo Include="<Name>.ComponentTests" />
</ItemGroup>
```

## Collection fixture

```csharp
// Collections/IntegrationTestCollection.cs
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<<Name>IntegrationFixture> { }
```

## Fixture template

The fixture starts a real Postgres container, builds a `WebApplicationFactory<Program>`,
strips all messaging, and replaces the DbContext.

```csharp
public class <Name>IntegrationFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public DatabaseResetter DbResetter { get; private set; } = null!;

    public HttpClient CreateAuthenticatedClient(string[] roles = null!)
    {
        roles ??= ["Admin"];
        var client = Factory.CreateClient();
        client.SetBearerToken(TestJwtTokenHelper.GenerateToken(roles: roles));
        return client;
    }

    public async Task InitializeAsync()
    {
        await Postgres.InitializeAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Config injection — must use environment variables with __ separator.
                // WebApplicationFactory's ConfigureAppConfiguration runs after Program.cs reads config.
                Environment.SetEnvironmentVariable("ConnectionStrings__<conn-name>", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("Jwt__Secret",   TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer",   TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);

                builder.ConfigureServices(services =>
                {
                    // 1. Replace DbContext
                    services.RemoveAll<DbContextOptions<<Name>DbContext>>();
                    services.RemoveAll<<Name>DbContext>();
                    services.AddDbContext<<Name>DbContext>(o =>
                        o.UseNpgsql(Postgres.ConnectionString).UseSnakeCaseNamingConvention());

                    // 2. Remove ALL MassTransit (RabbitMQ, Kafka Rider, sagas, outbox, consumers)
                    var toRemove = services
                        .Where(d =>
                            d.ServiceType.FullName?.Contains("MassTransit") == true ||
                            d.ImplementationType?.FullName?.Contains("MassTransit") == true ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // 3. Register mock Kafka producers for every ITopicProducer<T> the service uses
                    services.AddSingleton<ITopicProducer<SomeEvent>>(
                        _ => Mock.Of<ITopicProducer<SomeEvent>>());

                    // 4. Wire up the MassTransit in-process test harness
                    services.AddMassTransitTestHarness();

                    // 5. Override JWT validation to accept test tokens
                    services.Configure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme, options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(TestJwtTokenHelper.TestSecret)),
                                ValidateIssuer   = true, ValidIssuer   = TestJwtTokenHelper.TestIssuer,
                                ValidateAudience = true, ValidAudience = TestJwtTokenHelper.TestAudience,
                                ValidateLifetime = true, ClockSkew     = TimeSpan.Zero
                            };
                        });
                });
            });

        // Trigger host startup — important for services that call Database.Migrate() in Program.cs
        _ = Factory.Server;

        DbResetter = new DatabaseResetter(Postgres.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        // Clear env vars set in InitializeAsync
        Environment.SetEnvironmentVariable("ConnectionStrings__<conn-name>", null);
        // ... (mirror all SetEnvironmentVariable calls)
        await Postgres.DisposeAsync();
    }
}
```

## Test class template

```csharp
[Collection("Integration")]
[Trait("Category", "Integration")]
public class <Controller>Tests : IAsyncLifetime
{
    private readonly <Name>IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public <Controller>Tests(<Name>IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client  = fixture.CreateAuthenticatedClient();
    }

    public async Task InitializeAsync() => await _fixture.DbResetter.ResetAsync();
    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var response = await _client.GetAsync("/api/v1/<resource>");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<<Dto>>>>();
        result!.Data.Should().BeEmpty();
    }
}
```

## Known pitfalls and solutions

### Config must be injected as environment variables
`ConfigureAppConfiguration` runs **after** `Program.cs` has already read the config, so
`Environment.SetEnvironmentVariable("Key__SubKey", value)` with `__` double-underscore is the
only reliable way to override connection strings and secrets.

### MassTransit must be completely removed
The Kafka Rider tries to connect to a real broker and hangs indefinitely.
Remove every MassTransit descriptor via the LINQ filter shown above, then call
`services.AddMassTransitTestHarness()` to get an in-process loopback bus.

### JSONB columns require `EnableDynamicJson`
Services with `[Column(TypeName = "jsonb")]` properties (Sales, Financial) need an
`NpgsqlDataSourceBuilder` rather than a plain connection string:

```csharp
services.RemoveAll<NpgsqlDataSource>(); // also remove existing data-source descriptors
var dataSource = new NpgsqlDataSourceBuilder(Postgres.ConnectionString)
    .EnableDynamicJson()
    .Build();
services.AddSingleton(dataSource);
services.AddDbContext<<Name>DbContext>(o =>
    o.UseNpgsql(dataSource).UseSnakeCaseNamingConvention());
```

### Services that call `Database.Migrate()` at startup
Services like Financial and Orchestration run `Database.Migrate()` inside `Program.cs`.
**Do not** call `db.Database.EnsureCreated()` in the fixture — it will create the schema
without migration history, causing `Migrate()` to fail.
Instead, call `_ = Factory.Server;` after building the factory to force the host to start
(which runs the migration).

### Respawn — tables to ignore
For services with EF Core Outbox, add the infrastructure tables to `DatabaseResetter`:
```csharp
DbResetter = new DatabaseResetter(
    Postgres.ConnectionString,
    "__EFMigrationsHistory", "inbox_state", "outbox_message", "outbox_state");
```

### Soft deletes
Several entities use `IsActive = false` for deletes (Customer in Sales, Account/Budget in
Financial). Tests that call DELETE should assert `200 OK`, not follow up with `GET` expecting
`404`.

### Event namespaces
Most domain events live in `ERP.Contracts.Events.Domain`.
Saga completion events (`PurchaseCompleted`, `PurchaseFailed`, `ReturnCompleted`,
`ReturnFailed`) are in `ERP.Contracts.Events` (no `.Domain` suffix).
Use a `using` alias to keep things readable:
```csharp
using DomainEvents = ERP.Contracts.Events.Domain;
// or
using DomainEvents = ERP.Contracts.Events;
```

### WireMock (inter-service HTTP)
Services that call other services over HTTP (e.g. Inventory calling UserManagement) use
WireMock.Net. Start a server in the fixture, pass its URL as an env var, and set up stubs
in each test:
```csharp
// Fixture
WireMock = WireMockServer.Start();
Environment.SetEnvironmentVariable("Services__UserManagement", WireMock.Url);
// Test
_fixture.WireMock.Given(Request.Create().WithPath("/api/v1/users/me").UsingGet())
    .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(userData));
```

## Running tests

```bash
# Single suite
dotnet test services/inventory/InventoryManagement.IntegrationTests/

# All integration tests (from solution root)
dotnet test --filter "FullyQualifiedName~IntegrationTests" erp.sln
```
