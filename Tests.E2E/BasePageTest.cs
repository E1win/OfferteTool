using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Tests.E2E;

public class BasePageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = "https://localhost:7018",
            ViewportSize = new()
            {
                Width = 1920,
                Height = 1080
            }
        };
    }
}
