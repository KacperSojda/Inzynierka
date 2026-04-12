using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class ChatAiServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        private Mock<IConfiguration> CreateMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(c => c["Prompts:ResponseHelp"]).Returns("Pomoz odpowiedziec:");
            mockConfig.Setup(c => c["Prompts:CorrectMessage"]).Returns("Popraw bledy:");
            mockConfig.Setup(c => c["Prompts:Language"]).Returns("Rozpoznaj jezyk:");
            mockConfig.Setup(c => c["Prompts:Translate"]).Returns("Przetlumacz na {language}:");
            mockConfig.Setup(c => c["Prompts:Cenzure"]).Returns("Ocenzuruj to:");

            return mockConfig;
        }

        // TESTY DLA: CorrectMessageAsync //

        [Fact]
        public async Task CorrectMessageAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            mockGemini.Setup(g => g.AskAsync("Blendne zdanie", "Popraw bledy:"))
                      .ReturnsAsync("Bledne zdanie.");

            var service = new ChatAiService(context, mockGemini.Object, mockConfig.Object);

            var result = await service.CorrectMessageAsync("Blendne zdanie");

            Assert.Equal("Bledne zdanie.", result);
        }

        // TESTY DLA: CensorMessageAsync //

        [Fact]
        public async Task CensorMessageAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            mockGemini.Setup(g => g.AskAsync("Brzydkie slowo", "Ocenzuruj to:"))
                      .ReturnsAsync("*** slowo");

            var service = new ChatAiService(context, mockGemini.Object, mockConfig.Object);

            var result = await service.CensorMessageAsync("Brzydkie slowo");

            Assert.Equal("*** slowo", result);
        }

        // TESTY DLA: Generowania podpowiedzi z historią (ResponseHelp) //

        [Fact]
        public async Task GetPrivateResponseHelpAsync_FormatsHistoryAndCallsGemini()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            var userId = "ja";
            var friendId = "znajomy";

            var user1 = new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""};
            var user2 = new User {Id = friendId, UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""};
            context.Users.AddRange(user1, user2);

            context.Messages.AddRange(
                new Message {Id = 1, SenderId = friendId, ReceiverId = userId, Content = "Co tam?", DateTime = DateTime.UtcNow, Sender = user2},
                new Message {Id = 2, SenderId = userId, ReceiverId = friendId, Content = "Nic ciekawego", DateTime = DateTime.UtcNow.AddMinutes(2), Sender = user1}
            );
            await context.SaveChangesAsync();

            var expectedHistoryString = "[Znajomy] Co tam?, [user] Nic ciekawego";

            mockGemini.Setup(g => g.AskAsync(expectedHistoryString, "Pomoz odpowiedziec:"))
                      .ReturnsAsync("Zaproponuj spotkanie.");

            var service = new ChatAiService(context, mockGemini.Object, mockConfig.Object);

            var result = await service.GetPrivateResponseHelpAsync(userId, friendId);

            Assert.Equal("Zaproponuj spotkanie.", result);
        }

        // TESTY DLA: TranslatePrivateMessageAsync //

        [Fact]
        public async Task TranslatePrivateMessageAsync_DetectsLanguageBuildsPromptAndTranslates()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            var userId = "ja";
            var friendId = "znajomy";

            context.Messages.Add(new Message
            {
                Id = 1,
                SenderId = friendId,
                ReceiverId = userId,
                Content = "Hello",
                DateTime = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var expectedHistoryString = "Hello";

            mockGemini.Setup(g => g.AskAsync(expectedHistoryString, "Rozpoznaj jezyk:"))
                      .ReturnsAsync("Angielski");

            mockGemini.Setup(g => g.AskAsync("Czesc", "Przetlumacz na Angielski:"))
                      .ReturnsAsync("Hello there");

            var service = new ChatAiService(context, mockGemini.Object, mockConfig.Object);

            var result = await service.TranslatePrivateMessageAsync(userId, friendId, "Czesc");

            Assert.Equal("Hello there", result);
        }
    }
}