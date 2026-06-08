using System.Net;
using INZYNIERKA.Services.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace INZYNIERKA.Tests.Services
{
    public class GeminiServiceTests
    {
        private Mock<IConfiguration> CreateMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(c => c["ApiKeys:Gemini"])
                      .Returns("fake-api-key");

            mockConfig.Setup(c => c["EndPoints:Gemini"])
                      .Returns("https://fake-api.gemini.com/v1/models/gemini");

            return mockConfig;
        }

        private HttpClient CreateMockHttpClient(HttpResponseMessage responseToReturn, Exception? exceptionToThrow = null)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            var setup = handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );

            if (exceptionToThrow != null)
            {
                setup.ThrowsAsync(exceptionToThrow);
            }
            else
            {
                setup.ReturnsAsync(responseToReturn);
            }

            return new HttpClient(handlerMock.Object);
        }

        // TESTS FOR: AskAsync //

        [Fact]
        public async Task AskAsync_ReturnsEmptyString()
        {
            var mockConfig = CreateMockConfiguration();
            var httpClient = new HttpClient();
            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync(" ", " ");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task AskAsync_ReturnsParsedText()
        {
            var mockConfig = CreateMockConfiguration();

            var expectedJson = @"{
                ""candidates"": [{
                    ""content"": {
                        ""parts"": [
                        {
                            ""text"": ""AI generated response""
                            }
                        ]
                    }
                }]
            }";

            var fakeResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedJson)
            };

            var httpClient = CreateMockHttpClient(fakeResponse);
            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync("How are you?", "System prompt: ");

            Assert.Equal("AI generated response", result);
        }

        [Fact]
        public async Task AskAsync_ReturnsNull()
        {
            var mockConfig = CreateMockConfiguration();

            var fakeResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request format")
            };

            var httpClient = CreateMockHttpClient(fakeResponse);
            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync("Question?", "Prompt: ");

            Assert.Null(result);
        }

        [Fact]
        public async Task AskAsync_ReturnsNull_HttpClientException()
        {
            var mockConfig = CreateMockConfiguration();

            var networkException = new HttpRequestException("No connection to server.");
            var httpClient = CreateMockHttpClient(null, networkException);

            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync("Question?", "Prompt: ");

            Assert.Null(result);
        }
    }
}