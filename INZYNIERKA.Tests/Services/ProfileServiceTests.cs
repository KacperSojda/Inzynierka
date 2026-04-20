using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using INZYNIERKA.Services.ViewModels;
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

            var userId = "Ja";
            var user = new User
            {
                Id = userId,
                UserName = "Ja",
                PublicDescription = "Opis Publiczny",
                PrivateDescription = "Opis Prywatny",
                Avatar = "avatar.jpg"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.GetUserProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Ja", result.UserName);
            Assert.Equal("Opis Publiczny", result.PublicDescription);
            Assert.Equal("Opis Prywatny", result.PrivateDescription);
            Assert.Equal("avatar.jpg", result.Avatar);
        }

        [Fact]
        public async Task GetUserProfileAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetUserProfileAsync("nieistniejacy");

            Assert.Null(result);
        }

        // --- TESTY DLA GetOtherUserProfileAsync //

        [Fact]
        public async Task GetOtherUserProfileAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var userId = "Znajomy";
            var user = new User
            {
                Id = userId,
                UserName = "Znajomy",
                PublicDescription = "Publiczny opis",
                PrivateDescription = "Prywatny opis",
                Avatar = "avatar.jpg"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.GetOtherUserProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Znajomy", result.UserName);
            Assert.Equal("Publiczny opis", result.PublicDescription);
            Assert.Equal("", result.PrivateDescription);
        }

        [Fact]
        public async Task GetOtherUserProfileAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetOtherUserProfileAsync("nieistniejacy");

            Assert.Null(result);
        }

        // TESTY DLA GetUserProfileForEditAsync //

        [Fact]
        public async Task GetUserProfileForEditAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var userId = "Ja";
            dbContext.Users.Add(new User
            {
                Id = userId,
                UserName = "Ja",
                PublicDescription = "Publiczny Opis",
                PrivateDescription = "Prywatny Opis",
                Avatar = "avatar.jpg"
            });
            await dbContext.SaveChangesAsync();

            var result = await service.GetUserProfileForEditAsync(userId);

            Assert.NotNull(result);
            Assert.Equal("Ja", result.UserName);
            Assert.Equal("Prywatny Opis", result.PrivateDescription);
            Assert.Empty(result.Tags);
        }

        [Fact]
        public async Task GetUserProfileForEditAsyncTest2()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService(dbContext, mockUserManager.Object);

            var result = await service.GetUserProfileForEditAsync("nieistniejacy");

            Assert.Null(result);
        }

        // TESTY DLA: UpdateUserProfileAsync //

        [Fact]
        public async Task UpdateUserProfileAsyncTest()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var userId = "Ja";
            var existingUser = new User {Id = userId, UserName = "Ja"};

            mockUserManager.Setup(m => m.FindByIdAsync(userId))
                           .ReturnsAsync(existingUser);

            mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                           .ReturnsAsync(IdentityResult.Success);

            var service = new ProfileService(dbContext, mockUserManager.Object);

            var updateModel = new UserViewModel
            {
                Avatar = "Nowy avatar.jpg",
                PublicDescription = "Nowy Opis Publiczny",
                PrivateDescription = "Nowy Opis Prywatny"
            };

            var result = await service.UpdateUserProfileAsync(userId, updateModel);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);

            Assert.Equal("Nowy avatar.jpg", existingUser.Avatar);
            Assert.Equal("Nowy Opis Publiczny", existingUser.PublicDescription);
            Assert.Equal("Nowy Opis Prywatny", existingUser.PrivateDescription);
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

            var result = await service.UpdateUserProfileAsync("Nieistniejący", model);

            Assert.False(result.IsSuccess);
            Assert.Contains("Nie znaleziono użytkownika.", result.Errors);
        }
    }
}