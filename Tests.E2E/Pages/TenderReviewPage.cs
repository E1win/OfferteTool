using Microsoft.Playwright;

namespace Tests.E2E.Pages;

public sealed class TenderReviewPage
{
    private readonly IPage page;

    public TenderReviewPage(IPage page)
    {
        this.page = page;
    }

    public async Task ExpectOfferAsync(string tenderTitle, string supplierName)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = tenderTitle, Level = 1 }))
            .ToBeVisibleAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = $"Offerte van {supplierName}", Level = 2 }))
            .ToBeVisibleAsync();
    }

    public async Task ExpectAnswerAsync(string question, string answer)
    {
        await Assertions.Expect(QuestionSection(question).GetByText(answer, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task RateQuestionAsync(string question, string rating)
    {
        await QuestionSection(question)
            .GetByLabel($"Beoordeling voor {question}", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = rating });
    }

    public async Task SaveReviewAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Beoordeling opslaan" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert).GetByText("De beoordeling is opgeslagen.", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    public async Task ExpectRatingAsync(string question, string rating)
    {
        await Assertions.Expect(QuestionSection(question).GetByLabel($"Beoordeling voor {question}", new() { Exact = true }))
            .ToHaveValueAsync(rating);
    }

    private ILocator QuestionSection(string question) =>
        page.Locator(".review-question").Filter(new() { HasText = question });
}
