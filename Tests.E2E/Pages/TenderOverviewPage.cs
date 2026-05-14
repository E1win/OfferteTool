using Microsoft.Playwright;
using Tests.E2E.TestData;

namespace Tests.E2E.Pages;

public sealed class TenderOverviewPage
{
    private readonly IPage page;

    public TenderOverviewPage(IPage page)
    {
        this.page = page;
    }

    public async Task GoToAsync()
    {
        await page.GotoAsync("/Tender");
    }

    public async Task CreateTenderAsync(TenderDraft tender)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Nieuw offertetraject" }).ClickAsync();
        var createTenderModal = page.Locator("#createTenderModal");
        await createTenderModal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await createTenderModal.GetByLabel("Titel", new() { Exact = true }).FillAsync(tender.Title);
        await createTenderModal.GetByLabel("Beschrijving", new() { Exact = true }).FillAsync(tender.Description);
        await createTenderModal.GetByLabel("Einddatum", new() { Exact = true }).FillAsync(tender.EndDate.ToString("yyyy-MM-dd"));

        var publicCheckbox = createTenderModal.GetByLabel("Openbaar", new() { Exact = true });
        if (await publicCheckbox.IsCheckedAsync() != tender.IsPublic)
        {
            await publicCheckbox.SetCheckedAsync(tender.IsPublic);
        }

        await createTenderModal.GetByRole(AriaRole.Button, new() { Name = "Offertetraject aanmaken" }).ClickAsync();
    }

    public async Task OpenTenderDetailsAsync(string title)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = title, Exact = true }).ClickAsync();
    }
}
