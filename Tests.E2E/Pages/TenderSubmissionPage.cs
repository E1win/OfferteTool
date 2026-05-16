using Microsoft.Playwright;

namespace Tests.E2E.Pages;

public sealed class TenderSubmissionPage
{
    private readonly IPage page;

    public TenderSubmissionPage(IPage page)
    {
        this.page = page;
    }

    public async Task SubmitAnswersAsync(string textQuestion, string textAnswer, string numericQuestion, string numericAnswer, string choiceQuestion, string choiceOption)
    {
        await FillTextAnswerAsync(textQuestion, textAnswer);
        await FillNumericAnswerAsync(numericQuestion, numericAnswer);
        await SelectChoiceAnswerAsync(choiceQuestion, choiceOption);

        await page.GetByRole(AriaRole.Button, new() { Name = "Offerte indienen" }).ClickAsync();
    }

    public async Task ExpectSubmissionSucceededAsync()
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Alert).GetByText("Uw offerte is succesvol ingediend.", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    private async Task FillTextAnswerAsync(string question, string answer)
    {
        await QuestionSection(question).Locator("textarea, input").FillAsync(answer);
    }

    private async Task FillNumericAnswerAsync(string question, string answer)
    {
        await QuestionSection(question).Locator("input[type='number']").FillAsync(answer);
    }

    private async Task SelectChoiceAnswerAsync(string question, string option)
    {
        await QuestionSection(question).GetByText(option, new() { Exact = true }).ClickAsync();
    }

    private ILocator QuestionSection(string question) =>
        page.Locator(".submission-question").Filter(new() { HasText = question });
}
