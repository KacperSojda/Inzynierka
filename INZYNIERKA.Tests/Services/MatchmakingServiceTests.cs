using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class MatchmakingServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA: GetTagsForSearchAsync //

        [Fact]
        public async Task GetTagsForSearchAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag {Id = 1, Name = "Szachy"});
            context.Tags.Add(new Tag {Id = 2, Name = "Sport"});
            await context.SaveChangesAsync();

            var service = new MatchmakingService(context);

            var result = await service.GetTagsForSearchAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.AvailableTags.Count);
            Assert.All(result.AvailableTags, tag => Assert.False(tag.IsSelected));
            Assert.Contains(result.AvailableTags, t => t.TagName == "Szachy");
        }

        // TESTY DLA: GetUserForBrowserAsync //

        [Fact]
        public async Task GetUserForBrowserAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new MatchmakingService(context);

            var result = await service.GetUserForBrowserAsync("nieistniejacy");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserForBrowserAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Szukany";

            var user = new User
            {
                Id = userId,
                UserName = "Szukany",
                Avatar = "avatar.jpg",
                PublicDescription = "Publiczny opis",
                PrivateDescription = ""
            };

            var tag = new Tag {Id = 1, Name = "Szachy"};

            context.Users.Add(user);
            context.Tags.Add(tag);
            context.UserTags.Add(new UserTag {UserId = userId, TagId = tag.Id});
            await context.SaveChangesAsync();

            var service = new MatchmakingService(context);

            var result = await service.GetUserForBrowserAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Szukany", result.UserName);
            Assert.Equal("avatar.jpg", result.Avatar);
            Assert.Single(result.Tags);
            Assert.Equal("Szachy", result.Tags.First());
        }

        // TESTY DLA: GetMatchingUserIdsByTagsAsync //

        [Fact]
        public async Task GetMatchingUserIdsByTagsAsyncTest()
        {
            var context = CreateInMemoryDbContext();

            var userId = "Ja";
            var friendId = "Znajomy";
            var userMissingTagId = "Brakuje taga";
            var perfectMatchId = "Idealny kandydat";

            context.Users.AddRange(
                new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = friendId, UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = userMissingTagId, UserName = "Brakuje", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = perfectMatchId, UserName = "Idealny", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );

            var tag1 = new Tag {Id = 1, Name = "Szachy"};
            var tag2 = new Tag {Id = 2, Name = "Sport"};
            context.Tags.AddRange(tag1, tag2);

            context.UserFriends.Add(new UserFriend {UserId = userId, FriendId = friendId});

            context.UserTags.Add(new UserTag {UserId = friendId, TagId = 1});
            context.UserTags.Add(new UserTag {UserId = friendId, TagId = 2});

            context.UserTags.Add(new UserTag {UserId = userMissingTagId, TagId = 1});

            context.UserTags.Add(new UserTag {UserId = perfectMatchId, TagId = 1});
            context.UserTags.Add(new UserTag {UserId = perfectMatchId, TagId = 2});

            await context.SaveChangesAsync();
            var service = new MatchmakingService(context);
            var searchedTags = new List<int> {1, 2};

            var result = await service.GetMatchingUserIdsByTagsAsync(userId, searchedTags);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(perfectMatchId, result);
            Assert.Contains(userMissingTagId, result);
        }
    }
}