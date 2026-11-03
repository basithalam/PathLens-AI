using Npgsql;

namespace PathLens.IntegrationTests;

/// <summary>
/// The fixtures used to hardcode the local dev cluster (port 5433, trust auth), which is never the
/// address of the CI postgres service (5432, password) — so every fixture that touches a database
/// failed to connect in CI while passing on a developer machine. The server half now comes from the
/// runner's own <c>ConnectionStrings__DefaultConnection</c> when it sets one, and only the database
/// name stays per-fixture. Plain <c>dotnet test</c> locally keeps using 5433 with no configuration.
/// </summary>
internal static class TestDatabase
{
    private const string LocalDevCluster = "Host=localhost;Port=5433;Username=postgres";

    public static string Connection(string database)
    {
        var fromRunner = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        var builder = new NpgsqlConnectionStringBuilder(
            string.IsNullOrWhiteSpace(fromRunner) ? LocalDevCluster : fromRunner)
        {
            Database = database,
            Timeout = 5,
            CommandTimeout = 15,
        };

        return builder.ConnectionString;
    }
}
