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
        await page.GetByRole(AriaRole.Dialog, new() { Name = "Nieuw offertetraject aanmaken" }).WaitForAsync();

        await page.GetByLabel("Titel", new() { Exact = true }).FillAsync(tender.Title);
        await page.GetByLabel("Beschrijving", new() { Exact = true }).FillAsync(tender.Description);
        await page.GetByLabel("Einddatum", new() { Exact = true }).FillAsync(tender.EndDate.ToString("yyyy-MM-dd"));

        var publicCheckbox = page.GetByLabel("Openbaar", new() { Exact = true });
        if (await publicCheckbox.IsCheckedAsync() != tender.IsPublic)
        {
            await publicCheckbox.SetCheckedAsync(tender.IsPublic);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Offertetraject aanmaken" }).ClickAsync();
    }
}
