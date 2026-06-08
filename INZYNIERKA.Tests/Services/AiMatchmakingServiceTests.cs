using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class AiMatchmakingServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        private Mock<IConfiguration> CreateMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Prompts:Browser"]).Returns("Rate the match:");
            return mockConfig;
        }

        // TESTY DLA: GetPotentialMatchesForAiAsync //

        [Fact]
        public async Task AiMatches_ReturnUsers()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService<User>(context, mockGemini.Object, mockConfig.Object);

            var userId = "me";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Me", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "friend1", UserName = "Friend1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "friend2", UserName = "Friend2", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "stranger1", UserName = "Stranger1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "stranger2", UserName = "Stranger2", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = "friend1"},
                new UserFriend {UserId = "friend2", FriendId = userId}
            );
            await context.SaveChangesAsync();

            var result = await service.AiMatches(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("stranger1", result);
            Assert.Contains("stranger2", result);
            Assert.DoesNotContain(userId, result);
            Assert.DoesNotContain("friend1", result);
        }

        // TESTS FOR: AiNext //

        [Fact]
        public async Task AiNext_ReturnsNull_WhenUserNotExist()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService<User>(context, mockGemini.Object, mockConfig.Object);

            var result = await service.AiNext("nonexistent", new List<string> {"Nonexistent"}, 0);

            Assert.Null(result.MatchedUser);
            Assert.Equal(0, result.LastProcessedIndex);
        }

        [Fact]
        public async Task AiNext_ReturnsMatch()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService<User>(context, mockGemini.Object, mockConfig.Object);

            var userId = "me";
            var candidate1Id = "stranger1";
            var candidate2Id = "stranger2";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Me", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate1Id, UserName = "Stranger1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate2Id, UserName = "Stranger2", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );
            await context.SaveChangesAsync();

            var candidatesList = new List<string> { candidate1Id, candidate2Id };

            mockGemini.SetupSequence(g => g.AskAsync(It.IsAny<string>(), "Rate the match:"))
                      .ReturnsAsync("NO")
                      .ReturnsAsync("YES");

            var result = await service.AiNext(userId, candidatesList, 0);

            Assert.NotNull(result.MatchedUser);
            Assert.Equal("Stranger2", result.MatchedUser.UserName);
            Assert.Equal(2, result.LastProcessedIndex);
        }

        [Fact]
        public async Task AiNext_ReturnsNull_WhenNoMatch()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService<User>(context, mockGemini.Object, mockConfig.Object);

            var userId = "me";
            var candidate1Id = "stranger1";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Me", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate1Id, UserName = "Stranger1", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );
            await context.SaveChangesAsync();

            var candidatesList = new List<string> {candidate1Id};

            mockGemini.Setup(g => g.AskAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync("NO");

            var result = await service.AiNext(userId, candidatesList, 0);

            Assert.Null(result.MatchedUser);
            Assert.Equal(1, result.LastProcessedIndex);
        }
    }
}