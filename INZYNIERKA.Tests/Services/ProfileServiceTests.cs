using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class ProfileServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }
        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        // TESTY DLA GetUserProfileAsync //

        [Fact]
        public async Task GetUserProfileAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var userId = "test-id-123";
            var user = new User
            {
                Id = userId,
                UserName = "Inzynier",
                PublicDescription = "To jest opis publiczny",
                PrivateDescription = "To jest opis prywatny",
                Avatar = "avatar.jpg"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.GetUserProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Inzynier", result.UserName);
            Assert.Equal("To jest opis publiczny", result.PublicDescription);
            Assert.Equal("To jest opis prywatny", result.PrivateDescription);
            Assert.Equal("avatar.jpg", result.Avatar);
        }

        [Fact]
        public async Task GetUserProfileAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetUserProfileAsync("nieistniejace-id");

            Assert.Null(result);
        }

        // --- TESTY DLA GetOtherUserProfileAsync //

        [Fact]
        public async Task GetOtherUserProfileAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var userId = "inny-user-id";
            var user = new User
            {
                Id = userId,
                UserName = "KtosInny",
                PublicDescription = "Publiczny opis",
                PrivateDescription = "Prywatny opis - Nie powinno sie wyswietlic",
                Avatar = "avatar2.jpg"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.GetOtherUserProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("KtosInny", result.UserName);
            Assert.Equal("Publiczny opis", result.PublicDescription);
            Assert.Equal("", result.PrivateDescription);
        }

        [Fact]
        public async Task GetOtherUserProfileAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetOtherUserProfileAsync("nieistniejacy-id-123");

            Assert.Null(result);
        }

        // TESTY DLA GetUserProfileForEditAsync //

        [Fact]
        public async Task GetUserProfileForEditAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var userId = "moj-id";
            dbContext.Users.Add(new User
            {
                Id = userId,
                UserName = "Ja",
                PublicDescription = "Pub",
                PrivateDescription = "Priv",
                Avatar = "avatar.jpg"
            });
            await dbContext.SaveChangesAsync();

            var result = await service.GetUserProfileForEditAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Ja", result.UserName);
            Assert.Equal("Priv", result.PrivateDescription);
            Assert.Empty(result.Tags);
        }

        [Fact]
        public async Task GetUserProfileForEditAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetUserProfileForEditAsync("nieistniejacy-id-123");

            Assert.Null(result);
        }

        // TESTY DLA: UpdateUserProfileAsync //

        [Fact]
        public async Task UpdateUserProfileAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var userId = "istniejacy-id";
            var existingUser = new User { Id = userId, UserName = "StaryLogin" };

            mockUserManager.Setup(m => m.FindByIdAsync(userId))
                           .ReturnsAsync(existingUser);

            mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                           .ReturnsAsync(IdentityResult.Success);

            var service = new ProfileService(dbContext, mockUserManager.Object);

            var updateModel = new UserViewModel
            {
                Avatar = "nowy.jpg",
                PublicDescription = "Nowy publiczny",
                PrivateDescription = "Nowy prywatny"
            };

            var result = await service.UpdateUserProfileAsync(userId, updateModel);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);

            Assert.Equal("nowy.jpg", existingUser.Avatar);
            Assert.Equal("Nowy publiczny", existingUser.PublicDescription);
            Assert.Equal("Nowy prywatny", existingUser.PrivateDescription);
        }

        [Fact]
        public async Task UpdateUserProfileAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();

            mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                           .ReturnsAsync((User)null);

            var service = new ProfileService(dbContext, mockUserManager.Object);
            var model = new UserViewModel();

            var result = await service.UpdateUserProfileAsync("zly-id", model);

            Assert.False(result.IsSuccess);
            Assert.Contains("Nie znaleziono użytkownika.", result.Errors);
        }
    }
}