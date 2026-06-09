using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class ProfileE2ETests : PageTest
    {
        [Test]
        public async Task EditProfile()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Edit" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Public Description" }).ClearAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Public Description" }).FillAsync("New Public Description");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();
            await Expect(Page.GetByText("New Public Description")).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Edit" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Public Description" }).ClearAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Public Description" }).FillAsync("Test Public Description");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();
            await Expect(Page.GetByText("Test Public Description")).ToBeVisibleAsync();

        }

        [Test]
        public async Task EditTags()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Tags" }).ClickAsync();
            await Page.GetByText("szachy ✓").ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
            await Expect(Page.GetByText("szachy")).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Tags" }).ClickAsync();
            await Page.GetByText("szachy ✓").ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
            await Expect(Page.GetByText("szachy")).Not.ToBeVisibleAsync();
        }

        [Test]
        public async Task FriendListChat()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Friends" }).ClickAsync();
            await Page.GetByTitle("Chat").First.ClickAsync();
            await Expect(Page.GetByText("Ile to jest 2 + 2?")).ToBeVisibleAsync();
        }

        [Test]
        public async Task FriendListOtherProfile()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Profile" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Friends" }).ClickAsync();
            await Expect(Page.GetByText("Kacper")).ToBeVisibleAsync();
        }
    }
}