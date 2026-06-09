using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class NotificationsE2ETests : PageTest
    {
        [Test]
        public async Task Notificationlist()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Notifications" }).ClickAsync();
            await Expect(Page.GetByText("No notifications")).ToBeVisibleAsync();

        }
    }
}