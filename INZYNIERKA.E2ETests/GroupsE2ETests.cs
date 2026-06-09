using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class GroupsE2ETests : PageTest
    {
        [Test]
        public async Task UserGroupsList()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Groups" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Your Groups" }).ClickAsync();
            await Expect(Page.GetByText("Piłka nożna")).ToBeVisibleAsync();
        }

        [Test]
        public async Task CreateAndDeleteGroup()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Groups" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Create Group" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Group Name:" }).FillAsync("Testowa grupa");
            await Page.GetByRole(AriaRole.Button, new() { Name = " Create Group" }).ClickAsync();
            await Expect(Page.GetByText("Testowa grupa")).ToBeVisibleAsync();
            await Page.GetByTitle("Edit").Nth(1).ClickAsync();
            Page.Dialog += async (_, dialog) =>
            {
                Console.WriteLine($"Dialog message: {dialog.Message}");
                await dialog.AcceptAsync();
            };
            await Page.GetByRole(AriaRole.Button, new() { Name = " Delete group" }).ClickAsync();
            await Expect(Page.GetByText("Testowa grupa")).Not.ToBeVisibleAsync();
        }

        [Test]
        public async Task JoinAndLeaveGroup()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() { Name = " Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Tomek");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Groups" }).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() { Name = " Search Groups" }).ClickAsync();
            await Expect(Page.GetByText("Klub książki")).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = " Join" }).First.ClickAsync();
            await Expect(Page.GetByText("Klub książki")).ToBeVisibleAsync();
            await Page.GetByTitle("Leave").First.ClickAsync();
            await Expect(Page.GetByText("Klub książki")).Not.ToBeVisibleAsync();
        }
    }
}