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
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        // TESTS FOR: Profile //

        [Fact]
        public async Task Profile_ReturnsViewModel()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var userId = "me";
            var user = new User
            {
                Id = userId,
                UserName = "Me",
                PublicDescription = "Public Desc",
                PrivateDescription = "Private Desc",
                Avatar = "avatar.jpg",
                City = "Wroclaw"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.Profile(userId);

            Assert.NotNull(result);
            Assert.Equal("Me", result.UserName);
            Assert.Equal("Public Desc", result.PublicDescription);
            Assert.Equal("Private Desc", result.PrivateDescription);
            Assert.Equal("avatar.jpg", result.Avatar);
            Assert.Equal("Wroclaw", result.City);
        }

        [Fact]
        public async Task Profile_ReturnsNull()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var result = await service.Profile("nonexistent");

            Assert.Null(result);
        }

        // TESTS FOR: OtherProfile //

        [Fact]
        public async Task OtherProfile_ReturnsViewModel()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var userId = "friend";
            var user = new User
            {
                Id = userId,
                UserName = "Friend",
                PublicDescription = "Public Desc",
                PrivateDescription = "Secret Private Desc",
                Avatar = "avatar.jpg"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await service.OtherProfile(userId);

            Assert.NotNull(result);
            Assert.Equal("Friend", result.UserName);
            Assert.Equal("Public Desc", result.PublicDescription);
            Assert.Equal("", result.PrivateDescription);
        }

        [Fact]
        public async Task OtherProfile_ReturnsNull()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var result = await service.OtherProfile("nonexistent");

            Assert.Null(result);
        }

        // TESTS FOR: EditProfile //

        [Fact]
        public async Task EditProfile_ReturnsViewModel()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var userId = "me";
            dbContext.Users.Add(new User
            {
                Id = userId,
                UserName = "Me",
                PublicDescription = "Public Desc",
                PrivateDescription = "Private Desc",
                Avatar = "avatar.jpg"
            });
            await dbContext.SaveChangesAsync();

            var result = await service.EditProfile(userId);

            Assert.NotNull(result);
            Assert.Equal("Me", result.UserName);
            Assert.Equal("Private Desc", result.PrivateDescription);
            Assert.Empty(result.Tags);
        }

        [Fact]
        public async Task EditProfile_ReturnsNull()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var result = await service.EditProfile("nonexistent");

            Assert.Null(result);
        }

        // TESTS FOR: UpdateProfile //

        [Fact]
        public async Task UpdateProfile_UpdatesFieldsAndReturnsTrue()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var userId = "me";
            var existingUser = new User { Id = userId, UserName = "Me" };

            mockUserManager.Setup(m => m.FindByIdAsync(userId))
                           .ReturnsAsync(existingUser);

            mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                           .ReturnsAsync(IdentityResult.Success);

            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var testDate = new DateTime(1995, 5, 5);
            var updateModel = new UserViewModel
            {
                Avatar = "new_avatar.jpg",
                PublicDescription = "New Public Desc",
                PrivateDescription = "New Private Desc",
                City = "London",
                Country = "UK",
                Status = "Single",
                Zodiac = ZodiacSign.Taurus,
                Language = "English",
                BirthDate = testDate
            };

            var (result, err) = await service.UpdateProfile(userId, updateModel);

            Assert.True(result);
            Assert.Equal("new_avatar.jpg", existingUser.Avatar);
            Assert.Equal("New Public Desc", existingUser.PublicDescription);
            Assert.Equal("New Private Desc", existingUser.PrivateDescription);
            Assert.Equal("London", existingUser.City);
            Assert.Equal("UK", existingUser.Country);
            Assert.Equal("Single", existingUser.Status);
            Assert.Equal(ZodiacSign.Taurus, existingUser.Zodiac);
            Assert.Equal("English", existingUser.PreferredLanguages);
            Assert.Equal(DateTimeKind.Utc, existingUser.BirthDate?.Kind);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsFalse()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();

            mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                           .ReturnsAsync((User)null);

            var service = new ProfileService<User>(dbContext, mockUserManager.Object);
            var model = new UserViewModel();

            var (result, err) = await service.UpdateProfile("nonexistent", model);

            Assert.False(result);
            Assert.Equal("User not found", err);
        }

        // TESTS FOR: UpdateAvatar & UpdateCover //

        [Fact]
        public async Task UpdateAvatar_UpdatesAndReturnsTrue()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var userId = "me";
            var existingUser = new User { Id = userId };

            mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            mockUserManager.Setup(m => m.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);

            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var result = await service.UpdateAvatar(userId, "base64_avatar_data");

            Assert.True(result);
            Assert.Equal("base64_avatar_data", existingUser.Avatar);
        }

        [Fact]
        public async Task UpdateCover_UpdatesAndReturnsTrue()
        {
            var dbContext = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var userId = "me";
            var existingUser = new User { Id = userId };

            mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            mockUserManager.Setup(m => m.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);

            var service = new ProfileService<User>(dbContext, mockUserManager.Object);

            var result = await service.UpdateCover(userId, "base64_cover_data");

            Assert.True(result);
            Assert.Equal("base64_cover_data", existingUser.Cover);
        }
    }
}