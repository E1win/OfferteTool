namespace Tests.E2E
{
    public class LoginTests : BasePageTest
    {
        [Fact]
        public async Task LoginPageLoads()
        {
            await Page.GotoAsync("/Identity/Account/Login");
        }
    }
}
