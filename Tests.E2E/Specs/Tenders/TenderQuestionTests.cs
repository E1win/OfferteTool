using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderQuestionTests : BasePageTest
{
    [Fact]
    public async Task InkoperCanAddQuestionsToTender()
    {
        var tender = TenderTestData.CreateDraft("inkoper-add-questions");
        var textQuestion = $"{tender.Title} tekstvraag";
        var numericQuestion = $"{tender.Title} numerieke vraag";
        var choiceQuestion = $"{tender.Title} keuzevraag";

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var questionnaireEditorPage = new QuestionnaireEditorPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.CreateTenderAsync(tender);

        await questionnaireEditorPage.AddTextQuestionAsync(textQuestion, score: 30, rows: 4, maxLength: 500);
        await questionnaireEditorPage.AddNumericQuestionAsync(numericQuestion, score: 20, minValue: 1, maxValue: 100);
        await questionnaireEditorPage.AddChoiceQuestionAsync(choiceQuestion, score: 50, firstOption: "Ja", secondOption: "Nee");

        await Page.ReloadAsync();

        await questionnaireEditorPage.ExpectQuestionAsync(textQuestion);
        await questionnaireEditorPage.ExpectQuestionAsync(numericQuestion);
        await questionnaireEditorPage.ExpectQuestionAsync(choiceQuestion);
    }
}
