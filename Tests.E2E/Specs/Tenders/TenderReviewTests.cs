using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderReviewTests : BasePageTest
{
    private const string ReviewerName = "Pieter Bakker (beoordelaar@test.nl)";
    private const string SupplierName = "Leverancier B.V.";

    [Fact]
    public async Task BeoordelaarCanReviewOffer()
    {
        var tender = TenderTestData.CreateDraft("beoordelaar-review");
        var textQuestion = $"{tender.Title} tekstvraag";
        var numericQuestion = $"{tender.Title} numerieke vraag";
        var choiceQuestion = $"{tender.Title} keuzevraag";

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var questionnaireEditorPage = new QuestionnaireEditorPage(Page);
        var tenderSubmissionPage = new TenderSubmissionPage(Page);
        var tenderReviewPage = new TenderReviewPage(Page);

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
        await tenderReviewPage.ExpectOfferAsync(tender.Title, SupplierName);
        await tenderReviewPage.ExpectAnswerAsync(textQuestion, "Wij kunnen deze opdracht volledig uitvoeren.");
        await tenderReviewPage.ExpectAnswerAsync(numericQuestion, "42");
        await tenderReviewPage.ExpectAnswerAsync(choiceQuestion, "Ja");

        await tenderReviewPage.RateQuestionAsync(textQuestion, "Goed");
        await tenderReviewPage.RateQuestionAsync(numericQuestion, "Voldoende");
        await tenderReviewPage.RateQuestionAsync(choiceQuestion, "Uitstekend");
        await tenderReviewPage.SaveReviewAsync();

        await tenderReviewPage.ExpectRatingAsync(textQuestion, "Good");
        await tenderReviewPage.ExpectRatingAsync(numericQuestion, "Sufficient");
        await tenderReviewPage.ExpectRatingAsync(choiceQuestion, "Excellent");
    }
}
