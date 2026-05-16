namespace Tests.E2E.Fixtures;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class E2ECollection : ICollectionFixture<OfferteToolAppFixture>
{
    public const string Name = "E2E";
}
