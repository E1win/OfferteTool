using Microsoft.Playwright;
using Tests.E2E.TestData;

namespace Tests.E2E.Pages;

public sealed class LoginPage
{
    private readonly IPage page;

    public LoginPage(IPage page)
    {
        this.page = page;
    }

    public async Task LoginAsAsync(TestUser user)
    {
        await page.GotoAsync("/Identity/Account/Login");

        await page.GetByLabel("E-mailadres", new() { Exact = true }).FillAsync(user.Email);
        await page.GetByLabel("Wachtwoord", new() { Exact = true }).FillAsync(user.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Inloggen" }).ClickAsync();
    }
}
