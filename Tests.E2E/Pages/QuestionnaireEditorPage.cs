using Microsoft.Playwright;

namespace Tests.E2E.Pages;

public sealed class QuestionnaireEditorPage
{
    private readonly IPage page;
    private readonly ILocator editor;

    public QuestionnaireEditorPage(IPage page)
    {
        this.page = page;
        editor = page.Locator("#questionnaire-editor");
    }

    public async Task AddTextQuestionAsync(string text, int score, int rows, int maxLength)
    {
        var dialog = await OpenCreateDialogAsync();

        await dialog.GetByLabel("Vraag", new() { Exact = true }).FillAsync(text);
        await dialog.GetByLabel("Score", new() { Exact = true }).FillAsync(score.ToString());
        await dialog.GetByLabel("Vraagtype", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Tekstvraag" });
        await dialog.GetByLabel("Aantal regels", new() { Exact = true }).FillAsync(rows.ToString());
        await dialog.GetByLabel("Maximaal aantal tekens", new() { Exact = true }).FillAsync(maxLength.ToString());

        await SubmitDialogAsync(dialog);
        await ExpectQuestionAsync(text);
    }

    public async Task AddNumericQuestionAsync(string text, int score, int minValue, int maxValue)
    {
        var dialog = await OpenCreateDialogAsync();

        await dialog.GetByLabel("Vraag", new() { Exact = true }).FillAsync(text);
        await dialog.GetByLabel("Score", new() { Exact = true }).FillAsync(score.ToString());
        await dialog.GetByLabel("Vraagtype", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Numerieke vraag" });
        await dialog.GetByLabel("Minimumwaarde", new() { Exact = true }).FillAsync(minValue.ToString());
        await dialog.GetByLabel("Maximumwaarde", new() { Exact = true }).FillAsync(maxValue.ToString());

        await SubmitDialogAsync(dialog);
        await ExpectQuestionAsync(text);
    }

    public async Task AddChoiceQuestionAsync(string text, int score, string firstOption, string secondOption)
    {
        var dialog = await OpenCreateDialogAsync();

        await dialog.GetByLabel("Vraag", new() { Exact = true }).FillAsync(text);
        await dialog.GetByLabel("Score", new() { Exact = true }).FillAsync(score.ToString());
        await dialog.GetByLabel("Vraagtype", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Keuzevraag" });
        await dialog.GetByPlaceholder("Optie 1").FillAsync(firstOption);
        await dialog.GetByPlaceholder("Optie 2").FillAsync(secondOption);

        await SubmitDialogAsync(dialog);
        await ExpectQuestionAsync(text);
    }

    public async Task ExpectQuestionAsync(string text)
    {
        await Assertions.Expect(editor.GetByText(text, new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    private async Task<ILocator> OpenCreateDialogAsync()
    {
        await Assertions.Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Vraag toevoegen" }))
            .ToBeVisibleAsync();

        await editor.GetByRole(AriaRole.Button, new() { Name = "Vraag toevoegen" }).ClickAsync();

        var dialog = editor.GetByRole(AriaRole.Dialog, new() { Name = "Vraag toevoegen" });
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        return dialog;
    }

    private async Task SubmitDialogAsync(ILocator dialog)
    {
        await page.RunAndWaitForResponseAsync(
            async () => await dialog.GetByRole(AriaRole.Button, new() { Name = "Toevoegen", Exact = true }).ClickAsync(),
            response => response.Url.Contains("/questions", StringComparison.OrdinalIgnoreCase)
                && response.Request.Method == "POST"
                && response.Ok);

        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }
}
