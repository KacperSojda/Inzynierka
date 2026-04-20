using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class AiMatchmakingServiceTests
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
            mockConfig.Setup(c => c["Prompts:Browser"]).Returns("Ocen dopasowanie:");
            return mockConfig;
        }

        // TESTY DLA: GetPotentialMatchesForAiAsync //

        [Fact]
        public async Task GetPotentialMatchesForAiAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService(context, mockGemini.Object, mockConfig.Object);

            var userId = "ja";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "znajomy1", UserName = "Znajomy1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "znajomy2", UserName = "Znajomy2", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "nieznajomy1", UserName = "Nieznajomy1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "nieznajomy2", UserName = "Nieznajomy2", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = "znajomy1"},
                new UserFriend {UserId = "znajomy2", FriendId = userId}
            );
            await context.SaveChangesAsync();

            var result = await service.GetPotentialMatchesForAiAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("nieznajomy1", result);
            Assert.Contains("nieznajomy2", result);
            Assert.DoesNotContain(userId, result);
            Assert.DoesNotContain("znajomy1", result);
        }

        // TESTY DLA: FindNextAiMatchAsync //

        [Fact]
        public async Task FindNextAiMatchAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService(context, mockGemini.Object, mockConfig.Object);

            var result = await service.FindNextAiMatchAsync("nieistniejacy", new List<string> {"Nieistniejący"}, 0);

            Assert.Null(result.MatchedUser);
            Assert.Equal(0, result.LastProcessedIndex);
        }

        [Fact]
        public async Task FindNextAiMatchAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService(context, mockGemini.Object, mockConfig.Object);

            var userId = "ja";
            var candidate1Id = "nieznajomy1";
            var candidate2Id = "nieznajomy2";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate1Id, UserName = "Nieznajomy1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate2Id, UserName = "Nieznajomy2", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );
            await context.SaveChangesAsync();

            var candidatesList = new List<string> { candidate1Id, candidate2Id };

            mockGemini.SetupSequence(g => g.AskAsync(It.IsAny<string>(), "Ocen dopasowanie:"))
                      .ReturnsAsync("NO")
                      .ReturnsAsync("YES");

            var result = await service.FindNextAiMatchAsync(userId, candidatesList, 0);

            Assert.NotNull(result.MatchedUser);
            Assert.Equal("Nieznajomy2", result.MatchedUser.UserName);
            Assert.Equal(2, result.LastProcessedIndex);
        }

        [Fact]
        public async Task FindNextAiMatchAsyncTest3()
        {
            var context = CreateInMemoryDbContext();
            var mockConfig = CreateMockConfiguration();
            var mockGemini = new Mock<IGeminiService>();
            var service = new AiMatchmakingService(context, mockGemini.Object, mockConfig.Object);

            var userId = "ja";
            var candidate1Id = "Nieznajomy1";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = candidate1Id, UserName = "Nieznajomy1", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );
            await context.SaveChangesAsync();

            var candidatesList = new List<string> {candidate1Id};

            mockGemini.Setup(g => g.AskAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync("NO");

            var result = await service.FindNextAiMatchAsync(userId, candidatesList, 0);

            Assert.Null(result.MatchedUser);
            Assert.Equal(1, result.LastProcessedIndex);
        }
    }
}