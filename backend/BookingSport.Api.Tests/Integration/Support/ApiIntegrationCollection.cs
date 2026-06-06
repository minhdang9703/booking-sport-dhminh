namespace BookingSport.Api.Tests.Integration.Support;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiIntegrationCollection : ICollectionFixture<IntegrationTestFactory>
{
    public const string Name = "ApiIntegration";
}
