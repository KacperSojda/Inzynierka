using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class TagServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA GetAllTagsAsync //

        [Fact]
        public async Task GetAllTagsAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag {Id = 1, Name = "Szachy"});
            context.Tags.Add(new Tag {Id = 2, Name = "Sport"});
            await context.SaveChangesAsync();

            var service = new TagService(context);

            var result = await service.GetAllTagsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Name == "Szachy");
            Assert.Contains(result, t => t.Name == "Sport");
        }

        // TESTY DLA AddNewTagAsync //

        [Fact]
        public async Task AddNewTagAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new TagService(context);

            await service.AddNewTagAsync("NowyTag");

            var tags = await context.Tags.ToListAsync();
            Assert.Single(tags);
            Assert.Equal("NowyTag", tags.First().Name);
        }

        [Fact]
        public async Task AddNewTagAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            context.Tags.Add(new Tag {Id = 1, Name = "NowyTag"});
            await context.SaveChangesAsync();
            var service = new TagService(context);

            await service.AddNewTagAsync("NowyTag");

            var tags = await context.Tags.ToListAsync();
            Assert.Single(tags);
            Assert.Equal("NowyTag", tags.First().Name); 
        }

        // TESTY DLA: GetUserTagsForSelectionAsync //

        [Fact]
        public async Task GetUserTagsForSelectionAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";

            context.Tags.Add(new Tag {Id = 1, Name = "Szachy"});
            context.Tags.Add(new Tag {Id = 2, Name = "Sport"});

            context.UserTags.Add(new UserTag {UserId = userId, TagId = 1});
            await context.SaveChangesAsync();

            var service = new TagService(context);

            var result = await service.GetUserTagsForSelectionAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Tags.Count); 

            var chessTag = result.Tags.First(t => t.TagId == 1);
            var sportTag = result.Tags.First(t => t.TagId == 2);

            Assert.True(chessTag.IsSelected);
            Assert.False(sportTag.IsSelected);
        }

        // TESTY DLA: UpdateUserTagsAsync //

        [Fact]
        public async Task UpdateUserTagsAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";

            context.UserTags.Add(new UserTag {UserId = userId, TagId = 1});
            context.UserTags.Add(new UserTag {UserId = userId, TagId = 2});
            await context.SaveChangesAsync();

            var service = new TagService(context);

            var newTagIds = new List<int> {2, 3};

            await service.UpdateUserTagsAsync(userId, newTagIds);

            var userTags = await context.UserTags.Where(ut => ut.UserId == userId).ToListAsync();

            Assert.Equal(2, userTags.Count);
            Assert.Contains(userTags, ut => ut.TagId == 2);
            Assert.Contains(userTags, ut => ut.TagId == 3);
            Assert.DoesNotContain(userTags, ut => ut.TagId == 1);
        }

        [Fact]
        public async Task UpdateUserTagsAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";

            context.UserTags.Add(new UserTag {UserId = userId, TagId = 1});
            context.UserTags.Add(new UserTag {UserId = userId, TagId = 2});
            await context.SaveChangesAsync();

            var service = new TagService(context);

            await service.UpdateUserTagsAsync(userId, new List<int>());

            var userTags = await context.UserTags.Where(ut => ut.UserId == userId).ToListAsync();
            Assert.Empty(userTags);
        }
    }
}