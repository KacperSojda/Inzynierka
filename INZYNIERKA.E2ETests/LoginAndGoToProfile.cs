using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace INZYNIERKA.E2ETests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class LoginAndProfileTest : PageTest
    {
        [Test]
        public async Task LoginAndSeeProfile()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() {Name = " Log In"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Name"}).FillAsync("User1");
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Password"}).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() {Name = "Login"}).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() {Name = " Profile"}).ClickAsync();
            await Expect(Page.Locator("h1")).ToContainTextAsync("User1");
        }

        [Test]
        public async Task LoginAndEditProfile()
        {
            await Page.GotoAsync("http://localhost:8080/");
            await Page.GetByRole(AriaRole.Link, new() {Name = " Log In"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Name"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Name"}).FillAsync("User1");
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Password"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "Password"}).FillAsync("1234567890!");
            await Page.GetByRole(AriaRole.Button, new() {Name = "Login"}).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() {Name = " Profile"}).ClickAsync();
            await Page.GetByRole(AriaRole.Link, new() {Name = " Edit"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PublicDescription"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PublicDescription"}).ClearAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PublicDescription"}).FillAsync("Nowy Publiczny Opis");
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PrivateDescription"}).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PrivateDescription"}).ClearAsync();
            await Page.GetByRole(AriaRole.Textbox, new() {Name = "PrivateDescription"}).FillAsync("Nowy Prywatny Opis");
            await Page.GetByRole(AriaRole.Button, new() {Name = "Save changes"}).ClickAsync();
            await Expect(Page.Locator("h1")).ToContainTextAsync("User1");
            await Expect(Page.GetByText("Nowy Publiczny Opis")).ToBeVisibleAsync();
            await Expect(Page.GetByText("Nowy Prywatny Opis")).ToBeVisibleAsync();
        }
    }
}