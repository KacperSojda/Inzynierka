using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class MatchmakingServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: Tags //

        [Fact]
        public async Task Tags_ReturnsAllTags()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag { Id = 1, Name = "Chess" });
            context.Tags.Add(new Tag { Id = 2, Name = "Sports" });
            await context.SaveChangesAsync();

            var service = new MatchmakingService<User>(context);

            var result = await service.Tags();

            Assert.NotNull(result);
            Assert.Equal(2, result.AvailableTags.Count);
            Assert.All(result.AvailableTags, tag => Assert.False(tag.Selected));
            Assert.Contains(result.AvailableTags, t => t.TagName == "Chess");
        }

        // TESTS FOR: MatchedUser //

        [Fact]
        public async Task MatchedUser_ReturnsNull()
        {
            var context = CreateInMemoryDbContext();
            var service = new MatchmakingService<User>(context);

            var result = await service.MatchedUser("nonexistent_id");

            Assert.Null(result);
        }

        [Fact]
        public async Task MatchedUser_ReturnsViewModel()
        {
            var context = CreateInMemoryDbContext();
            var userId = "target_user";

            var user = new User
            {
                Id = userId,
                UserName = "TargetUser",
                Avatar = "avatar.jpg",
                PublicDescription = "Public description",
                PrivateDescription = ""
            };

            var tag = new Tag { Id = 1, Name = "Chess" };

            context.Users.Add(user);
            context.Tags.Add(tag);
            context.UserTags.Add(new UserTag { UserId = userId, TagId = tag.Id });
            await context.SaveChangesAsync();

            var service = new MatchmakingService<User>(context);

            var result = await service.MatchedUser(userId);

            Assert.NotNull(result);
            Assert.Equal("TargetUser", result.UserName);
            Assert.Equal("avatar.jpg", result.Avatar);
            Assert.Single(result.Tags);
            Assert.Equal("Chess", result.Tags.First());
        }

        // TESTS FOR: MatchingUsersIds //

        [Fact]
        public async Task MatchingUsersIds_ReturnsMatches_BasedOnTags()
        {
            var context = CreateInMemoryDbContext();

            var userId = "me";
            var friendId = "friend";
            var partialMatchId = "partial_match";
            var perfectMatchId = "perfect_match";

            context.Users.AddRange(
                new User { Id = userId, UserName = "Me", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" },
                new User { Id = friendId, UserName = "Friend", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" },
                new User { Id = partialMatchId, UserName = "Partial Match", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" },
                new User { Id = perfectMatchId, UserName = "Perfect Match", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" }
            );

            var tag1 = new Tag { Id = 1, Name = "Chess" };
            var tag2 = new Tag { Id = 2, Name = "Sports" };
            context.Tags.AddRange(tag1, tag2);

            context.UserFriends.Add(new UserFriend { UserId = userId, FriendId = friendId });

            context.UserTags.Add(new UserTag { UserId = friendId, TagId = 1 });
            context.UserTags.Add(new UserTag { UserId = friendId, TagId = 2 });

            context.UserTags.Add(new UserTag { UserId = partialMatchId, TagId = 1 });

            context.UserTags.Add(new UserTag { UserId = perfectMatchId, TagId = 1 });
            context.UserTags.Add(new UserTag { UserId = perfectMatchId, TagId = 2 });

            await context.SaveChangesAsync();
            var service = new MatchmakingService<User>(context);
            var searchedTags = new List<int> { 1, 2 };

            var result = await service.MatchingUsersIds(userId, searchedTags);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(perfectMatchId, result);
            Assert.Contains(partialMatchId, result);
            Assert.DoesNotContain(friendId, result);
        }

        [Fact]
        public async Task MatchingUsersIds_ReturnsMatches_BasedOnFilters()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            var user1 = new User { Id = "user1", UserName = "John Doe", City = "Warsaw", Country = "Poland", PrivateDescription = "PrivateDescription1", PublicDescription = "PublicDescription1", Avatar = "DefaultAvatar1" };
            var user2 = new User { Id = "user2", UserName = "Max Mustermann", City = "Berlin", Country = "Germany", PrivateDescription = "PrivateDescription2", PublicDescription = "PublicDescription2", Avatar = "DefaultAvatar2" };

            context.Users.AddRange(
                new User { Id = userId, UserName = "Me", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" },
                user1,
                user2
            );

            await context.SaveChangesAsync();
            var service = new MatchmakingService<User>(context);

            var result = await service.MatchingUsersIds(userId, null, "john", "warsaw", "poland");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("user1", result.First());
        }
    }
}