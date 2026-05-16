using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderComparisonTests : BasePageTest
{
    private const string ReviewerName = "Pieter Bakker (beoordelaar@test.nl)";
    private const string SupplierName = "Leverancier B.V.";

    [Fact]
    public async Task InkoperCanCompareSubmittedOffers()
    {
        var tender = TenderTestData.CreateDraft("inkoper-compare");
        var textQuestion = $"{tender.Title} tekstvraag";
        var numericQuestion = $"{tender.Title} numerieke vraag";
        var choiceQuestion = $"{tender.Title} keuzevraag";

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var questionnaireEditorPage = new QuestionnaireEditorPage(Page);
        var tenderSubmissionPage = new TenderSubmissionPage(Page);
        var tenderReviewPage = new TenderReviewPage(Page);
        var tenderComparisonPage = new TenderComparisonPage(Page);
        var tenderSubmissionComparisonPage = new TenderSubmissionComparisonPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.CreateTenderAsync(tender);
        await tenderDetailsPage.AssignReviewerAsync(ReviewerName);
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
        await loginPage.LogoutAsync();

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(tender.Title);
        await tenderDetailsPage.CloseTenderAsync();
        await loginPage.LogoutAsync();

        await loginPage.LoginAsAsync(TestUsers.Beoordelaar);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(tender.Title);
        await tenderDetailsPage.GoToReviewFormAsync(SupplierName);
        await tenderReviewPage.RateQuestionAsync(textQuestion, "Goed");
        await tenderReviewPage.RateQuestionAsync(numericQuestion, "Voldoende");
        await tenderReviewPage.RateQuestionAsync(choiceQuestion, "Uitstekend");
        await tenderReviewPage.SaveReviewAsync();
        await loginPage.LogoutAsync();

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(tender.Title);
        await tenderDetailsPage.CompleteTenderAsync();
        await tenderDetailsPage.GoToComparisonDashboardAsync();

        await tenderComparisonPage.ExpectDashboardAsync(tender.Title);
        await tenderComparisonPage.ExpectSubmissionAsync(SupplierName, "86 / 100", "86%", "1 / 2");
        await tenderComparisonPage.OpenSubmissionAsync(SupplierName);

        await tenderSubmissionComparisonPage.ExpectSubmissionAsync(SupplierName, tender.Title, "86 / 100", "1 / 2");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(textQuestion, "Wij kunnen deze opdracht volledig uitvoeren.", ReviewerName, "Goed", "24 / 30");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(numericQuestion, "42", ReviewerName, "Voldoende", "12 / 20");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(choiceQuestion, "Ja", ReviewerName, "Uitstekend", "50 / 50");
    }
}
