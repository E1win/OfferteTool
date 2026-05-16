using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.Seeding;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderComparisonTests : BasePageTest
{
    [Fact]
    public async Task InkoperCanCompareSubmittedOffers()
    {
        var tender = TenderTestData.CreateDraft("inkoper-compare");
        var scenario = await new TenderScenarioSeeder()
            .CreateCompletedTenderWithReviewedSubmissionAsync(tender);

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var tenderComparisonPage = new TenderComparisonPage(Page);
        var tenderSubmissionComparisonPage = new TenderSubmissionComparisonPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.OpenTenderDetailsAsync(scenario.Tender.Title);
        await tenderDetailsPage.GoToComparisonDashboardAsync();

        await tenderComparisonPage.ExpectDashboardAsync(scenario.Tender.Title);
        await tenderComparisonPage.ExpectSubmissionAsync(scenario.SupplierName, "86 / 100", "86%", "1 / 2");
        await tenderComparisonPage.OpenSubmissionAsync(scenario.SupplierName);

        await tenderSubmissionComparisonPage.ExpectSubmissionAsync(scenario.SupplierName, scenario.Tender.Title, "86 / 100", "1 / 2");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(scenario.Questions.TextQuestion, scenario.Answers.TextAnswer, scenario.ReviewerName, "Goed", "24 / 30");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(scenario.Questions.NumericQuestion, scenario.Answers.NumericAnswer, scenario.ReviewerName, "Voldoende", "12 / 20");
        await tenderSubmissionComparisonPage.ExpectQuestionReviewAsync(scenario.Questions.ChoiceQuestion, scenario.Answers.ChoiceAnswer, scenario.ReviewerName, "Uitstekend", "50 / 50");
    }
}
