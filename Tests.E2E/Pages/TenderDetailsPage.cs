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
        await ExpectStatusAsync("Open");
    }

    public async Task CloseTenderAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Offertetraject sluiten" }).ClickAsync();

        var closeTenderModal = page.Locator("#closeTenderModal");
        await closeTenderModal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await closeTenderModal.GetByRole(AriaRole.Button, new() { Name = "Offertetraject sluiten", Exact = true }).ClickAsync();
        await ExpectStatusAsync("Gesloten");
    }

    public async Task AssignReviewerAsync(string reviewerName)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Beoordelaars wijzigen" }).ClickAsync();

        var reviewerAssignmentModal = page.Locator("#reviewerAssignmentModal");
        await reviewerAssignmentModal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await reviewerAssignmentModal.GetByLabel(reviewerName, new() { Exact = true }).SetCheckedAsync(true);
        await reviewerAssignmentModal.GetByRole(AriaRole.Button, new() { Name = "Beoordelaars opslaan" }).ClickAsync();

        await reviewerAssignmentModal.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Assertions.Expect(page.Locator("aside .card").GetByText(reviewerName, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task GoToSubmissionFormAsync()
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Offerte indienen" }).ClickAsync();
    }

    public async Task GoToReviewFormAsync(string supplierName)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = supplierName, Exact = true }).ClickAsync();
    }

    public async Task ExpectStatusAsync(string status)
    {
        await Assertions.Expect(page.Locator("dl").Filter(new() { HasText = "Status" }).GetByText(status, new() { Exact = true }))
            .ToBeVisibleAsync();
    }
}
