using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class LoginAndProfileTest : PageTest
    {
        [Test]
        public async Task User_Should_Be_Able_To_Login_And_See_Profile()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("User1");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Expect(Page.Locator("h1")).ToContainTextAsync("User1");
        }
    }
}