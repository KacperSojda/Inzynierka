using INZYNIERKA.Services.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class FileServiceTests
    {

        [Fact]
        public async Task UploadFile_ReturnsFalse_FileIsNull()
        {
            var service = new FileService();

            var result = await service.UploadFile(null);

            Assert.False(result.Result);
            Assert.Equal("File is empty.", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFile_ReturnsFalse_FileIsEmpty()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(0);

            var result = await service.UploadFile(fileMock.Object);

            Assert.False(result.Result);
            Assert.Equal("File is empty.", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFile_ReturnsFalse_InvalidExtension()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("avatar.exe");

            var result = await service.UploadFile(fileMock.Object);

            Assert.False(result.Result);
            Assert.Equal("Unsupported file format. Allowed formats: .jpg, .jpeg, .png", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFile_ReturnsFalse_TooLargeFile()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            long threeMegabytes = 3 * 1024 * 1024;
            fileMock.Setup(f => f.Length).Returns(threeMegabytes);
            fileMock.Setup(f => f.FileName).Returns("avatar.png");

            var result = await service.UploadFile(fileMock.Object);

            Assert.False(result.Result);
            Assert.Equal("File is too large. Maximum size is 2MB.", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFile_ReturnsTrueAndBase64String()
        {
            var service = new FileService();
            var fileMock = new Mock<IFormFile>();

            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("avatar.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var dummyBytes = new byte[] { 1, 2, 3 };

            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Callback<Stream, CancellationToken>((stream, token) => stream.Write(dummyBytes, 0, dummyBytes.Length))
                    .Returns(Task.CompletedTask);

            var result = await service.UploadFile(fileMock.Object);

            Assert.True(result.Result);
            Assert.Equal("data:image/jpeg;base64,AQID", result.ErrorMessage);
        }
    }
}