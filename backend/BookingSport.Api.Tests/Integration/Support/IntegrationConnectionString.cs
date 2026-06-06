using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BookingSport.Api.Tests.Integration.Support;

public static class IntegrationConnectionString
{
    public const string TestDatabaseName = "booking_sport_integration_tests";

    public static string FromDevelopmentSettings()
    {
        var apiProjectDirectory = ResolveApiProjectDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectDirectory)
            .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured in appsettings.Development.json.");
        }

        return UseTestDatabase(connectionString);
    }

    public static string UseTestDatabase(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = TestDatabaseName
        };

        EnsureSafeTestDatabaseName(builder.Database);

        return builder.ConnectionString;
    }

    public static void EnsureSafeTestDatabaseName(string? databaseName)
    {
        if (!string.Equals(databaseName, TestDatabaseName, StringComparison.OrdinalIgnoreCase) &&
            !databaseName?.EndsWith("_integration_tests", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException(
                $"Refusing to reset database '{databaseName}'. Integration tests may only use '{TestDatabaseName}' or a database ending with '_integration_tests'.");
        }
    }

    public static async Task EnsureDatabaseExistsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        EnsureSafeTestDatabaseName(builder.Database);
        var databaseName = builder.Database
            ?? throw new InvalidOperationException("Integration test database name is not configured.");

        if (await CanConnectAsync(builder.ConnectionString, cancellationToken))
        {
            return;
        }

        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

        if (exists)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string MaskPassword(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "***";
        }

        return builder.ConnectionString;
    }

    private static async Task<bool> CanConnectAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            return false;
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    private static string ResolveApiProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "BookingSport.Api", "appsettings.Development.json");

            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            candidate = Path.Combine(current.FullName, "backend", "BookingSport.Api", "appsettings.Development.json");

            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find backend/BookingSport.Api/appsettings.Development.json.");
    }
}
