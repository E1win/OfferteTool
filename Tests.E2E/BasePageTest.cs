using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Tests.E2E.Fixtures;

namespace Tests.E2E;

public abstract class BasePageTest : PageTest
{
    private readonly OfferteToolAppFixture app;

    public BasePageTest(OfferteToolAppFixture app)
    {
        this.app = app;
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            ViewportSize = new()
            {
                Width = 1920,
                Height = 1080
            },
            BaseURL = app.BaseUrl
        };
    }
}
