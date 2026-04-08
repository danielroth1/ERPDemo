using System.Data.Common;
using Npgsql;
using Respawn;

namespace ERP.Testing.Shared.Fixtures;

/// <summary>
/// Wraps Respawn to efficiently reset database state between tests.
/// Call ResetAsync() in each test's InitializeAsync/constructor.
/// </summary>
public class DatabaseResetter
{
    private Respawner? _respawner;
    private readonly string _connectionString;
    private readonly string[] _tablesToIgnore;

    public DatabaseResetter(string connectionString, params string[] tablesToIgnore)
    {
        _connectionString = connectionString;
        _tablesToIgnore = tablesToIgnore;
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        if (_respawner is null)
        {
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = _tablesToIgnore
                    .Select(t => new Respawn.Graph.Table(t))
                    .ToArray()
            });
        }

        await _respawner.ResetAsync(connection);
    }
}
