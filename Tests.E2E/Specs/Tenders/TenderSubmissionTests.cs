using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

public class TenderSubmissionTests : BasePageTest
{
    [Fact]
    public async Task LeverancierCanSubmitOffer()
    {
        var tender = TenderTestData.CreateDraft("leverancier-submit");
        var textQuestion = $"{tender.Title} tekstvraag";
        var numericQuestion = $"{tender.Title} numerieke vraag";
        var choiceQuestion = $"{tender.Title} keuzevraag";

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var questionnaireEditorPage = new QuestionnaireEditorPage(Page);
        var tenderSubmissionPage = new TenderSubmissionPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.CreateTenderAsync(tender);
        await questionnaireEditorPage.AddTextQuestionAsync(textQuestion, score: 30, rows: 4, maxLength: 500);
        await questionnaireEditorPage.AddNumericQuestionAsync(numericQuestion, score: 20, minValue: 1, maxValue: 100);
        await questionnaireEditorPage.AddChoiceQuestionAsync(choiceQuestion, score: 50, firstOption: "Ja", secondOption: "Nee");
        await tenderDetailsPage.OpenTenderAsync();
        await loginPage.LogoutAsync();

        await loginPage.LoginAsAsync(TestUsers.Leverancier);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(tender.Title);
        await tenderDetailsPage.GoToSubmissionFormAsync();
        await tenderSubmissionPage.SubmitAnswersAsync(
            textQuestion,
            "Wij kunnen deze opdracht volledig uitvoeren.",
            numericQuestion,
            "42",
            choiceQuestion,
            "Ja");

        await tenderSubmissionPage.ExpectSubmissionSucceededAsync();
    }
}
