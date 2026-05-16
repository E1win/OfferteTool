using Tests.E2E;
using Tests.E2E.Pages;
using Tests.E2E.Fixtures;
using Tests.E2E.TestData;

namespace Tests.E2E.Specs.Tenders;

[Collection(E2ECollection.Name)]
public class TenderCreationTests : BasePageTest
{
    [Fact]
    public async Task InkoperCanCreateTender()
    {
        var tender = TenderTestData.CreateDraft("inkoper-create");
        var loginPage = new LoginPage(Page);
        var tenderOverviewPage = new TenderOverviewPage(Page);
        var tenderDetailsPage = new TenderDetailsPage(Page);

        await loginPage.LoginAsAsync(TestUsers.Inkoper);
        await tenderOverviewPage.GoToAsync();
        await tenderOverviewPage.CreateTenderAsync(tender);

        await tenderDetailsPage.ExpectTenderAsync(tender);
    }
}
