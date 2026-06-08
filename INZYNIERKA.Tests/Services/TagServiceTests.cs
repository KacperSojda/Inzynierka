using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class TagServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: AllTags //

        [Fact]
        public async Task ReturnsAllTags()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag { Id = 1, Name = "Chess" });
            context.Tags.Add(new Tag { Id = 2, Name = "Sports" });
            await context.SaveChangesAsync();

            var service = new TagService<User>(context);

            var result = await service.AllTags();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Name == "Chess");
            Assert.Contains(result, t => t.Name == "Sports");
        }

        // TESTS FOR: NewTag //

        [Fact]
        public async Task NewTag_ReturnsFalse()
        {
            var context = CreateInMemoryDbContext();
            var service = new TagService<User>(context);

            var (result, errorMessage) = await service.NewTag("   ");

            Assert.False(result);
            Assert.Equal("Tag name cannot be empty", errorMessage);
        }

        [Fact]
        public async Task NewTag_AddsTagAndReturnsTrue()
        {
            var context = CreateInMemoryDbContext();
            var service = new TagService<User>(context);

            var (result, errorMessage) = await service.NewTag("NewTag");

            Assert.True(result);
            Assert.Empty(errorMessage);

            var tags = await context.Tags.ToListAsync();
            Assert.Single(tags);
            Assert.Equal("NewTag", tags.First().Name);
        }

        [Fact]
        public async Task NewTag_ReturnsFalse_TagAlreadyExists()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag { Id = 1, Name = "ExistingTag" });
            await context.SaveChangesAsync();
            var service = new TagService<User>(context);

            var (result, errorMessage) = await service.NewTag("existingtag");

            Assert.False(result);
            Assert.Equal("Tag already exists", errorMessage);

            var tags = await context.Tags.ToListAsync();
            Assert.Single(tags);
        }

        // TESTS FOR: UserTags //

        [Fact]
        public async Task UserTags_ReturnsEmptyList()
        {
            var context = CreateInMemoryDbContext();
            var service = new TagService<User>(context);

            var result = await service.UserTags("");

            Assert.NotNull(result);
            Assert.Empty(result.Tags);
        }

        [Fact]
        public async Task UserTags_ReturnsTags()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            context.Tags.Add(new Tag { Id = 1, Name = "Chess" });
            context.Tags.Add(new Tag { Id = 2, Name = "Sports" });

            context.UserTags.Add(new UserTag { UserId = userId, TagId = 1 });
            await context.SaveChangesAsync();

            var service = new TagService<User>(context);

            var result = await service.UserTags(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Tags.Count);

            var chessTag = result.Tags.First(t => t.TagId == 1);
            var sportTag = result.Tags.First(t => t.TagId == 2);

            Assert.True(chessTag.Selected);
            Assert.False(sportTag.Selected);
        }

        // TESTS FOR: UpdateUserTags //

        [Fact]
        public async Task UpdateUserTags_UserIdIsEmpty()
        {
            var context = CreateInMemoryDbContext();
            var service = new TagService<User>(context);
            var newTagIds = new List<int> { 1, 2 };

            await service.UpdateUserTags("", newTagIds);

            var userTags = await context.UserTags.ToListAsync();
            Assert.Empty(userTags);
        }

        [Fact]
        public async Task UpdateUserTags_AddsAndRemovesTags()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            context.UserTags.Add(new UserTag { UserId = userId, TagId = 1 });
            context.UserTags.Add(new UserTag { UserId = userId, TagId = 2 });
            await context.SaveChangesAsync();

            var service = new TagService<User>(context);

            var newTagIds = new List<int> { 2, 3 };

            await service.UpdateUserTags(userId, newTagIds);

            var userTags = await context.UserTags.Where(ut => ut.UserId == userId).ToListAsync();

            Assert.Equal(2, userTags.Count);
            Assert.Contains(userTags, ut => ut.TagId == 2);
            Assert.Contains(userTags, ut => ut.TagId == 3);
            Assert.DoesNotContain(userTags, ut => ut.TagId == 1);
        }

        [Fact]
        public async Task UpdateUserTags_ClearsAllTags()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            context.UserTags.Add(new UserTag { UserId = userId, TagId = 1 });
            context.UserTags.Add(new UserTag { UserId = userId, TagId = 2 });
            await context.SaveChangesAsync();

            var service = new TagService<User>(context);

            await service.UpdateUserTags(userId, new List<int>());

            var userTags = await context.UserTags.Where(ut => ut.UserId == userId).ToListAsync();
            Assert.Empty(userTags);
        }
    }
}