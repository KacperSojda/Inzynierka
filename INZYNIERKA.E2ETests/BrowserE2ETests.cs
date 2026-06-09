using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class BrowserE2ETests : PageTest
    {
        [Test]
        public async Task MatchByTagsAndRequest()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Browser" }).ClickAsync();
            await Page.GetByText("Muzyka ✓").ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = " Search" }).ClickAsync();
            await Expect(Page.GetByText("User2").First).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = " Add" }).ClickAsync();
            await Expect(Page.GetByText("no more users")).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Requests" }).ClickAsync();
            await Expect(Page.GetByText("User2")).ToBeVisibleAsync();
            await Page.GetByTitle("Delete").ClickAsync();
            await Expect(Page.GetByText("No invitations sent")).ToBeVisibleAsync();

        }

        [Test]
        public async Task NoMatch()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Browser" }).ClickAsync();
            await Page.GetByText("Gry komputerowe ✓").ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = " Search" }).ClickAsync();
            await Expect(Page.GetByText("no more users")).ToBeVisibleAsync();
        }

        [Test]
        public async Task MatchByFilter()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Browser" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "User Name" }).FillAsync("User2");
            await Page.GetByRole(AriaRole.Button, new() { Name = " Search" }).ClickAsync();
            await Expect(Page.GetByText("User2").First).ToBeVisibleAsync();
        }
    }
}