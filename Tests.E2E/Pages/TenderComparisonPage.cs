using Microsoft.Playwright;

namespace Tests.E2E.Pages;

public sealed class TenderComparisonPage
{
    private readonly IPage page;

    public TenderComparisonPage(IPage page)
    {
        this.page = page;
    }

    public async Task ExpectDashboardAsync(string tenderTitle)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Vergelijkingsdashboard", Level = 1 }))
            .ToBeVisibleAsync();

        await Assertions.Expect(page.GetByText(tenderTitle, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task ExpectSubmissionAsync(string supplierName, string score, string percentage, string reviewers)
    {
        var row = SubmissionRow(supplierName);

        await Assertions.Expect(row.GetByText(score, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(row.GetByText(percentage, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(row.GetByText(reviewers, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task OpenSubmissionAsync(string supplierName)
    {
        await SubmissionRow(supplierName)
            .GetByRole(AriaRole.Link, new() { Name = $"Offerte van {supplierName} openen" })
            .ClickAsync();
    }

    private ILocator SubmissionRow(string supplierName) =>
        page.Locator("tr.comparison-row").Filter(new() { HasText = supplierName });
}
