using System.Net;
using INZYNIERKA.Services;
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

        private HttpClient CreateMockHttpClient(HttpResponseMessage responseToReturn, Exception exceptionToThrow = null)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            var setup = handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );

            if (exceptionToThrow != null) setup.ThrowsAsync(exceptionToThrow);

            else setup.ReturnsAsync(responseToReturn);

            return new HttpClient(handlerMock.Object);
        }

        // TEST 1: Puste pytanie //

        [Fact]
        public async Task AskAsyncTest()
        {
            var mockConfig = CreateMockConfiguration();
            var httpClient = new HttpClient();
            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync(" ", "Prompt: ");

            Assert.Equal("The question cannot be empty.", result);
        }

        // TEST 2: Udana odpowiedź z API //

        [Fact]
        public async Task AskAsyncTest2()
        {
            var mockConfig = CreateMockConfiguration();

            var expectedJson = @"{
                ""candidates"": [{
                    ""content"": {
                        ""parts"": [
                        {
                            ""text"": ""Odpowiedz""
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

            var result = await service.AskAsync("Pytanie?", "Pytanie: ");

            Assert.Equal("Odpowiedz", result);
        }

        // TEST 3: Błąd HTTP z API //

        [Fact]
        public async Task AskAsync_WhenApiReturnsErrorStatusCode_ReturnsFormattedErrorString()
        {
            var mockConfig = CreateMockConfiguration();

            var fakeResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Zly format zapytania")
            };

            var httpClient = CreateMockHttpClient(fakeResponse);
            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync("Pytanie?", "Prompt: ");

            Assert.Equal("Błąd API (Status: 400): Zly format zapytania", result);
        }

        // TEST 4: Wyjątek podczas łączenia z API //

        [Fact]
        public async Task AskAsync_WhenHttpClientThrowsException_ReturnsCaughtErrorMessage()
        {
            var mockConfig = CreateMockConfiguration();

            var networkException = new HttpRequestException("Brak polaczenia z serwerem.");
            var httpClient = CreateMockHttpClient(null, networkException);

            var service = new GeminiService(mockConfig.Object, httpClient);

            var result = await service.AskAsync("Pytanie?", "Prompt: ");

            Assert.Equal("Błąd Gemini: Brak polaczenia z serwerem.", result);
        }
    }
}