using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
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

            mockConfig.Setup(c => c["Prompts:ResponseHelp"]).Returns("Help reply:");
            mockConfig.Setup(c => c["Prompts:CorrectMessage"]).Returns("Correct errors:");
            mockConfig.Setup(c => c["Prompts:Language"]).Returns("Detect language:");
            mockConfig.Setup(c => c["Prompts:Translate"]).Returns("Translate to {language}:");
            mockConfig.Setup(c => c["Prompts:Censor"]).Returns("Censor this:");
            mockConfig.Setup(c => c["Prompts:SummarizeChat"]).Returns("Summarize this chat:");
            mockConfig.Setup(c => c["Prompts:StyleBase"]).Returns("Use standard style.");
            mockConfig.Setup(c => c["Prompts:Casual"]).Returns("Use casual style.");

            return mockConfig;
        }

        // TESTS FOR: CorrectMessage //

        [Fact]
        public async Task CorrectMessage_ReturnsString()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            mockGemini.Setup(g => g.AskAsync("Bad sentence", "Correct errors:"))
                      .ReturnsAsync("Correct sentence.");

            var service = new ChatAiService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.CorrectMessage("Bad sentence");

            Assert.Equal("Correct sentence.", result);
        }

        // TESTS FOR: CensorMessage //

        [Fact]
        public async Task CensorMessage_ReturnsString()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            mockGemini.Setup(g => g.AskAsync("Bad word", "Censor this:"))
                      .ReturnsAsync("*** word");

            var service = new ChatAiService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.CensorMessage("Bad word");

            Assert.Equal("*** word", result);
        }

        // TESTS FOR: Generating suggestions with history (ResponseHelp) //

        [Fact]
        public async Task ResponseHelp_ReturnsString()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            var userId = "me";
            var friendId = "friend";

            var user1 = new User { Id = userId, UserName = "Me", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            var user2 = new User { Id = friendId, UserName = "Friend", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            context.Users.AddRange(user1, user2);

            context.UserFriends.Add(new UserFriend { UserId = userId, FriendId = friendId, Tone = "casual" });

            context.Messages.AddRange(
                new Message { Id = 1, SenderId = friendId, ReceiverId = userId, Content = "What's up?", Timestamp = DateTime.UtcNow, Sender = user2 },
                new Message { Id = 2, SenderId = userId, ReceiverId = friendId, Content = "Nothing much", Timestamp = DateTime.UtcNow.AddMinutes(2), Sender = user1 }
            );
            await context.SaveChangesAsync();

            mockGemini.Setup(g => g.AskAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync("Suggest a meeting.\nAsk about their day.");

            var service = new ChatAiService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.ResponseHelp(userId, friendId);

            Assert.Equal(2, result.Count);
            Assert.Equal("Suggest a meeting.", result[0]);
            Assert.Equal("Ask about their day.", result[1]);
        }

        // TESTS FOR: TranslateMessage //

        [Fact]
        public async Task TranslateMessage_ReturnsTranslatedMessage()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            var userId = "me";
            var friendId = "friend";

            context.Messages.Add(new Message
            {
                Id = 1,
                SenderId = friendId,
                ReceiverId = userId,
                Content = "Hello",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            mockGemini.Setup(g => g.AskAsync("Hello", "Detect language:"))
                      .ReturnsAsync("English");

            mockGemini.Setup(g => g.AskAsync("Czesc", "Translate to English:"))
                      .ReturnsAsync("Hello there");

            var service = new ChatAiService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.TranslateMessage(userId, friendId, "Czesc");

            Assert.Equal("Hello there", result);
        }

        // TESTS FOR: SummarizeChat //

        [Fact]
        public async Task SummarizeChat_ReturnsSummary()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();

            var userId = "me";
            var friendId = "friend";

            var user1 = new User { Id = userId, UserName = "Me", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription" , Avatar = "DefaultAvatar" };
            var user2 = new User { Id = friendId, UserName = "Friend", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription" , Avatar = "DefaultAvatar" };
            context.Users.AddRange(user1, user2);

            var today = DateTime.UtcNow;

            context.Messages.AddRange(
                new Message { Id = 1, SenderId = friendId, ReceiverId = userId, Content = "Hi", Timestamp = today, Sender = user2 },
                new Message { Id = 2, SenderId = userId, ReceiverId = friendId, Content = "Hello", Timestamp = today.AddMinutes(5), Sender = user1 }
            );
            await context.SaveChangesAsync();

            mockGemini.Setup(g => g.AskAsync(It.IsAny<string>(), "Summarize this chat:"))
                      .ReturnsAsync("User and Friend said hi to each other.");

            var service = new ChatAiService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.SummarizeChat(userId, friendId, today.AddDays(-1), today.AddDays(1));

            Assert.Equal("User and Friend said hi to each other.", result);
        }
    }
}