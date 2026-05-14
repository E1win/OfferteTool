namespace Tests.E2E.Fixtures;

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<OfferteToolAppFixture>
{
    public const string Name = "E2E";
}
