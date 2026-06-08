using BookingSport.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace BookingSport.Api.Tests.Integration.Support;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "BookingSport.IntegrationTests";
    public const string JwtAudience = "BookingSport.IntegrationTests";
    public const string JwtSecret = "booking-sport-integration-test-secret-with-enough-length";

    private PostgreSqlContainer? postgres;
    private string? activeConnectionString;
    private IntegrationDatabaseMode databaseMode = IntegrationDatabaseMode.Unavailable;
    private string? localPostgresUnavailableReason;
    private string? dockerUnavailableReason;
    private readonly Dictionary<string, string?> previousEnvironmentValues = new();

    public bool IsIntegrationDatabaseAvailable => !string.IsNullOrWhiteSpace(activeConnectionString);
    public IntegrationDatabaseMode DatabaseMode => databaseMode;
    public string ActiveConnectionStringForReport => activeConnectionString is null
        ? string.Empty
        : IntegrationConnectionString.MaskPassword(activeConnectionString);
    public string IntegrationDatabaseUnavailableReason =>
        $"Local PostgreSQL unavailable: {localPostgresUnavailableReason ?? "not checked"}. " +
        $"Testcontainers unavailable: {dockerUnavailableReason ?? "not checked"}.";

    public async Task InitializeAsync()
    {
        if (await TryUseLocalPostgresAsync())
        {
            ApplyEnvironmentConfiguration();
            return;
        }

        try
        {
            postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("booking_sport_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await postgres.StartAsync();
            activeConnectionString = postgres.GetConnectionString();
            databaseMode = IntegrationDatabaseMode.Testcontainers;
        }
        catch (DockerUnavailableException exception)
        {
            dockerUnavailableReason = exception.Message;
        }

        if (IsIntegrationDatabaseAvailable)
        {
            ApplyEnvironmentConfiguration();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }

        RestoreEnvironmentConfiguration();
    }

    public async Task ResetDatabaseAsync()
    {
        EnsureIntegrationDatabaseAvailable();
        EnsureSafeActiveDatabase();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        EnsureIntegrationDatabaseAvailable();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = GetActiveConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:AccessTokenMinutes"] = "120",
                ["Auth:RefreshTokenDays"] = "14",
                ["Auth:RefreshCookieName"] = "bookingSport.refresh",
                ["Auth:CookieSecure"] = "true",
                ["Auth:CookieSameSite"] = "Lax",
                ["Hangfire:Enabled"] = "false",
                ["Logging:File:Enabled"] = "false",
                ["ApiLogging:EnableRequestBodyLogging"] = "true",
                ["Cors:AllowedOrigins:0"] = "http://localhost"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(GetActiveConnectionString()));
        });
    }

    private async Task<bool> TryUseLocalPostgresAsync()
    {
        try
        {
            var connectionString = IntegrationConnectionString.FromDevelopmentSettings();
            await IntegrationConnectionString.EnsureDatabaseExistsAsync(connectionString);

            activeConnectionString = connectionString;
            databaseMode = IntegrationDatabaseMode.LocalPostgres;

            return true;
        }
        catch (Exception exception) when (exception is Npgsql.NpgsqlException or InvalidOperationException or DirectoryNotFoundException)
        {
            localPostgresUnavailableReason = exception.Message;
            return false;
        }
    }

    private string GetActiveConnectionString()
    {
        EnsureIntegrationDatabaseAvailable();

        return activeConnectionString!;
    }

    private void EnsureIntegrationDatabaseAvailable()
    {
        if (string.IsNullOrWhiteSpace(activeConnectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL is required for integration tests but no database is available. " +
                IntegrationDatabaseUnavailableReason);
        }
    }

    private void EnsureSafeActiveDatabase()
    {
        IntegrationConnectionString.EnsureSafeTestDatabaseName(
            new Npgsql.NpgsqlConnectionStringBuilder(GetActiveConnectionString()).Database);
    }

    private void ApplyEnvironmentConfiguration()
    {
        SetEnvironmentVariable("ConnectionStrings__DefaultConnection", GetActiveConnectionString());
        SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        SetEnvironmentVariable("Jwt__AccessTokenMinutes", "120");
        SetEnvironmentVariable("Auth__RefreshTokenDays", "14");
        SetEnvironmentVariable("Auth__RefreshCookieName", "bookingSport.refresh");
        SetEnvironmentVariable("Auth__CookieSecure", "true");
        SetEnvironmentVariable("Auth__CookieSameSite", "Lax");
        SetEnvironmentVariable("Hangfire__Enabled", "false");
        SetEnvironmentVariable("Logging__File__Enabled", "false");
        SetEnvironmentVariable("ApiLogging__EnableRequestBodyLogging", "true");
        SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        previousEnvironmentValues.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }

    private void RestoreEnvironmentConfiguration()
    {
        foreach (var (key, value) in previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        previousEnvironmentValues.Clear();
    }
}

public enum IntegrationDatabaseMode
{
    Unavailable,
    LocalPostgres,
    Testcontainers
}
