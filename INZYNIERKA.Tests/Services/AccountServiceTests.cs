using INZYNIERKA.Domain.Constants;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Security.Claims;

namespace INZYNIERKA.Tests.Services
{
    public class AccountServiceTests
    {
        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<User>> CreateMockSignInManager(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            var context = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "testuser") }));
            context.User = claimsPrincipal;
            contextAccessor.Setup(a => a.HttpContext).Returns(context);

            return new Mock<SignInManager<User>>(userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        private IMemoryCache CreateRealMemoryCache()
        {
            return new MemoryCache(new MemoryCacheOptions());
        }

        // TESTS FOR: Login //

        [Fact]
        public async Task Login_ReturnsSuccess()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockSignInManager.Setup(s => s.PasswordSignInAsync("user", "password", false, false))
                             .ReturnsAsync(SignInResult.Success);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new LoginViewModel { Name = "user", Password = "password", RememberMe = false };

            var result = await service.Login(model);

            Assert.True(result.Result);
            Assert.False(result.IsLockedOut);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public async Task Login_ReturnsLockedOut()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockSignInManager.Setup(s => s.PasswordSignInAsync("user", "password", false, false))
                             .ReturnsAsync(SignInResult.LockedOut);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new LoginViewModel { Name = "user", Password = "password" };

            var result = await service.Login(model);

            Assert.False(result.Result);
            Assert.True(result.IsLockedOut);
            Assert.Equal("Your account is Locked.", result.ErrorMessage);
        }

        [Fact]
        public async Task Login_ReturnsFailure()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockSignInManager.Setup(s => s.PasswordSignInAsync("user", "wrong", false, false))
                             .ReturnsAsync(SignInResult.Failed);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new LoginViewModel { Name = "user", Password = "wrong" };

            var result = await service.Login(model);

            Assert.False(result.Result);
            Assert.False(result.IsLockedOut);
            Assert.Equal("Wrong username or password.", result.ErrorMessage);
        }

        // TESTS FOR: Register //

        [Fact]
        public async Task Register_ReturnsSuccess()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), "password123"))
                           .ReturnsAsync(IdentityResult.Success);

            mockSignInManager.Setup(s => s.SignInAsync(It.IsAny<User>(), false, null))
                             .Returns(Task.CompletedTask);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new RegisterViewModel { Name = "newUser", Email = "test@test.com", Password = "password123" };

            var (result, errors) = await service.Register(model);

            Assert.True(result);
            Assert.Empty(errors);

            mockUserManager.Verify(u => u.CreateAsync(It.Is<User>(user =>
                user.UserName == "newUser" &&
                user.Email == "test@test.com" &&
                user.Avatar == AvatarConsts.DefaultAvatar &&
                user.PublicDescription == "PublicDescription"
            ), "password123"), Times.Once);
        }

        [Fact]
        public async Task Register_ReturnsErrors()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            var identityErrors = new[] { new IdentityError { Description = "Password too weak" } };
            mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), "weak"))
                           .ReturnsAsync(IdentityResult.Failed(identityErrors));

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new RegisterViewModel { Name = "newUser", Email = "test@test.com", Password = "weak" };

            var (result, errors) = await service.Register(model);

            Assert.False(result);
            Assert.Contains("Password too weak", errors);
        }

        // TESTS FOR: VerifyEmail //

        [Fact]
        public async Task VerifyEmail_ReturnsTrue()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockUserManager.Setup(u => u.FindByEmailAsync("nonexistent@test.com"))
                           .ReturnsAsync((User)null);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new VerifyEmailViewModel { Email = "nonexistent@test.com" };

            var (result, errorMessage) = await service.VerifyEmail(model);

            Assert.True(result);
            Assert.Null(errorMessage);
        }

        [Fact]
        public async Task VerifyEmail_SendsEmailAndSetsCache()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();
            var email = "test@test.com";

            mockUserManager.Setup(u => u.FindByEmailAsync(email))
                           .ReturnsAsync(new User { Email = email });

            mockEmailService.Setup(e => e.SendEmail(email, "Reset Password", It.IsAny<string>()))
                            .ReturnsAsync(true);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new VerifyEmailViewModel { Email = email };

            var (result, errorMessage) = await service.VerifyEmail(model);

            Assert.True(result);
            Assert.Null(errorMessage);
            Assert.NotNull(cache.Get($"OTP{email}"));
        }

        // TESTS FOR: ChangePassword //

        [Fact]
        public async Task ChangePassword_ReturnsFalse()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new ChangePasswordViewModel { Email = "test@test.com", OtpCode = "123456", NewPassword = "NewPassword1" };

            var (result, errors) = await service.ChangePassword(model);

            Assert.False(result);
            Assert.Contains("The code is invalid or has expired.", errors);
        }

        [Fact]
        public async Task ChangePassword_ReturnsTrues()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();
            var email = "test@test.com";
            var otp = "123456";

            cache.Set($"OTP{email}", otp);

            var user = new User { Email = email };
            mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            mockUserManager.Setup(u => u.ResetPasswordAsync(user, "reset-token", "NewPassword1")).ReturnsAsync(IdentityResult.Success);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);
            var model = new ChangePasswordViewModel { Email = email, OtpCode = otp, NewPassword = "NewPassword1" };

            var (result, errors) = await service.ChangePassword(model);

            Assert.True(result);
            Assert.Empty(errors);
            Assert.Null(cache.Get($"OTP{email}"));
        }

        // TESTS FOR: DeleteAccount //

        [Fact]
        public async Task DeleteAccount_ReturnsFalse()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync(new User { Id = "loggedInUser" });

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);

            var targetUser = new User { Id = "otherUser" };

            var (result, errorMessage) = await service.DeleteAccount(targetUser);

            Assert.False(result);
            Assert.Equal("You can only delete your own account.", errorMessage);
        }

        [Fact]
        public async Task DeleteAccount_LocksAccountAndSignsOut()
        {
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
            var mockEmailService = new Mock<IEmailService>();
            var cache = CreateRealMemoryCache();

            var user = new User { Id = "me" };

            mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync(user);

            mockUserManager.Setup(u => u.SetLockoutEnabledAsync(user, true))
                           .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(u => u.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
                           .ReturnsAsync(IdentityResult.Success);

            mockSignInManager.Setup(s => s.SignOutAsync())
                             .Returns(Task.CompletedTask);

            var service = new AccountService<User>(mockSignInManager.Object, mockUserManager.Object, mockEmailService.Object, cache);

            var (result, errorMessage) = await service.DeleteAccount(user);

            // Assert
            Assert.True(result);
            Assert.Null(errorMessage);
            mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
        }
    }
}