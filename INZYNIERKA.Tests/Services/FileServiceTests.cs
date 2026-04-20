using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class FileServiceTests
    {
        // TEST 1: Brak pliku //

        [Fact]
        public async Task UploadAvatarAsyncTest()
        {
            var service = new FileService();

            var result = await service.UploadAvatarAsync(null);

            Assert.False(result.IsSuccess);
            Assert.Equal("Nie wybrano pliku.", result.Result);
        }

        // TEST 2: Pusty plik //

        [Fact]
        public async Task UploadAvatarAsyncTest2()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(0);

            var result = await service.UploadAvatarAsync(fileMock.Object);

            Assert.False(result.IsSuccess);
            Assert.Equal("Nie wybrano pliku.", result.Result);
        }

        // TEST 3: Nieprawidłowe rozszerzenie //

        [Fact]
        public async Task UploadAvatarAsyncTest3()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("avatar.exe");

            var result = await service.UploadAvatarAsync(fileMock.Object);

            Assert.False(result.IsSuccess);
            Assert.Equal("Nieobsługiwany format pliku. Dozwolone formaty: .jpg, .jpeg, .png", result.Result);
        }

        // TEST 4: Plik jest za duży //

        [Fact]
        public async Task UploadAvatarAsyncTest4()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            long threeMegabytes = 3 * 1024 * 1024;
            fileMock.Setup(f => f.Length).Returns(threeMegabytes);
            fileMock.Setup(f => f.FileName).Returns("avatar.png");

            var result = await service.UploadAvatarAsync(fileMock.Object);

            Assert.False(result.IsSuccess);
            Assert.Equal("Plik jest zbyt duży. Maksymalny rozmiar to 2MB.", result.Result);
        }

        // TEST 5: Poprawny plik //

        [Fact]
        public async Task UploadAvatarAsyncTest5()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(1024 * 1024);
            fileMock.Setup(f => f.FileName).Returns("avatar.jpg");

            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var result = await service.UploadAvatarAsync(fileMock.Object);

            Assert.True(result.IsSuccess);

            Assert.StartsWith("/uploads/avatars/", result.Result);
            Assert.EndsWith(".jpg", result.Result);

            var fileName = result.Result.Replace("/uploads/avatars/", "").Replace(".jpg", "");
            Assert.True(Guid.TryParse(fileName, out _), "Nazwa pliku powinna być prawidłowym identyfikatorem GUID.");
        }
    }
}