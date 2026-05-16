using Microsoft.Playwright;

namespace Tests.E2E.Pages;

public sealed class TenderSubmissionComparisonPage
{
    private readonly IPage page;

    public TenderSubmissionComparisonPage(IPage page)
    {
        this.page = page;
    }

    public async Task ExpectSubmissionAsync(string supplierName, string tenderTitle, string score, string reviewers)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = $"Offerte van {supplierName}", Level = 1 }))
            .ToBeVisibleAsync();

        await Assertions.Expect(page.GetByText(tenderTitle, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(SummaryCard("Totaalscore").GetByText(score, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(SummaryCard("Beoordelaars").GetByText(reviewers, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task ExpectQuestionReviewAsync(string question, string answer, string reviewerName, string rating, string score)
    {
        var questionCard = page.Locator("article.card").Filter(new() { HasText = question });

        await Assertions.Expect(questionCard.GetByText(answer, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(questionCard.GetByText(reviewerName, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(questionCard.GetByText(rating, new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(questionCard.GetByText(score, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    private ILocator SummaryCard(string title) =>
        page.Locator(".card").Filter(new() { HasText = title });
}
