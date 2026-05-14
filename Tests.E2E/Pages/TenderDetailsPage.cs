using Microsoft.Playwright;
using Tests.E2E.TestData;

namespace Tests.E2E.Pages;

public sealed class TenderDetailsPage
{
    private readonly IPage page;

    public TenderDetailsPage(IPage page)
    {
        this.page = page;
    }

    public async Task ExpectTenderAsync(TenderDraft tender)
    {
        var mainContent = page.Locator("main");

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = tender.Title, Level = 1 }))
            .ToBeVisibleAsync();

        await Assertions.Expect(mainContent.Locator("article").GetByText(tender.Description))
            .ToBeVisibleAsync();

        await ExpectStatusAsync("Ontwerp");

        await Assertions.Expect(page.Locator("dl").Filter(new() { HasText = "Publiek" }).GetByText(tender.IsPublic ? "Ja" : "Nee", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task OpenTenderAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Offertetraject openen" }).ClickAsync();

        var openTenderModal = page.Locator("#openTenderModal");
        await openTenderModal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await openTenderModal.GetByRole(AriaRole.Button, new() { Name = "Offertetraject openen", Exact = true }).ClickAsync();
    }

    public async Task ExpectStatusAsync(string status)
    {
        await Assertions.Expect(page.Locator("dl").Filter(new() { HasText = "Status" }).GetByText(status, new() { Exact = true }))
            .ToBeVisibleAsync();
    }
}
