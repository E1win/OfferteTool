using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.Seeding;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderReviewTests : BasePageTest
{
    [Fact]
    public async Task BeoordelaarCanReviewOffer()
    {
        var tender = TenderTestData.CreateDraft("beoordelaar-review");
        var scenario = await new TenderScenarioSeeder()
            .CreateClosedTenderWithSubmittedOfferAsync(tender);

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var tenderReviewPage = new TenderReviewPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Beoordelaar);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(scenario.Tender.Title);
        await tenderDetailsPage.GoToReviewFormAsync(scenario.SupplierName);
        await tenderReviewPage.ExpectOfferAsync(scenario.Tender.Title, scenario.SupplierName);
        await tenderReviewPage.ExpectAnswerAsync(scenario.Questions.TextQuestion, scenario.Answers.TextAnswer);
        await tenderReviewPage.ExpectAnswerAsync(scenario.Questions.NumericQuestion, scenario.Answers.NumericAnswer);
        await tenderReviewPage.ExpectAnswerAsync(scenario.Questions.ChoiceQuestion, scenario.Answers.ChoiceAnswer);

        await tenderReviewPage.RateQuestionAsync(scenario.Questions.TextQuestion, "Goed");
        await tenderReviewPage.RateQuestionAsync(scenario.Questions.NumericQuestion, "Voldoende");
        await tenderReviewPage.RateQuestionAsync(scenario.Questions.ChoiceQuestion, "Uitstekend");
        await tenderReviewPage.SaveReviewAsync();

        await tenderReviewPage.ExpectRatingAsync(scenario.Questions.TextQuestion, "Good");
        await tenderReviewPage.ExpectRatingAsync(scenario.Questions.NumericQuestion, "Sufficient");
        await tenderReviewPage.ExpectRatingAsync(scenario.Questions.ChoiceQuestion, "Excellent");
    }
}
