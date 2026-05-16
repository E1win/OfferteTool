using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderStatusTests : BasePageTest
{
    [Fact]
    public async Task InkoperCanOpenTender()
    {
        var tender = TenderTestData.CreateDraft("inkoper-open");
        var question = $"{tender.Title} publicatievraag";

        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);
        var questionnaireEditorPage = new QuestionnaireEditorPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.CreateTenderAsync(tender);
        await questionnaireEditorPage.AddTextQuestionAsync(question, score: 10, rows: 2, maxLength: 250);

        await tenderDetailsPage.OpenTenderAsync();

        await tenderDetailsPage.ExpectStatusAsync("Open");
    }
}
