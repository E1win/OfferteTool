using Tests.E2E.Fixtures;

namespace Tests.E2E;

[Collection(E2ECollection.Name)]
public class LoginTests : BasePageTest
{
    public LoginTests(OfferteToolAppFixture app)
        : base(app)
    {
    }

    [Fact]
    public async Task LoginPageLoads()
    {
        await Page.GotoAsync("/Identity/Account/Login");
    }
}
