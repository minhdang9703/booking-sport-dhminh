namespace BookingSport.Api.Tests.Integration.Support;

public static class IntegrationTestFactoryExtensions
{
    public static void SkipIfDockerUnavailable(this IntegrationTestFactory factory)
    {
        Skip.IfNot(
            factory.IsIntegrationDatabaseAvailable,
            "PostgreSQL is required for integration tests but no local PostgreSQL or Testcontainers database is available. " +
            factory.IntegrationDatabaseUnavailableReason);
    }
}
