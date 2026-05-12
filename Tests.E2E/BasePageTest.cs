using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Tests.E2E
{
    public class BasePageTest : PageTest
    {
        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                ViewportSize = new()
                {
                    Width = 1920,
                    Height = 1080
                },
                // So we only need to use relative URLs in the tests
                BaseURL = "https://localhost:7018/",
            };
        }
    }
}
