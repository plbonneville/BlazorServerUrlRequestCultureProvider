using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BlazorServerUrlRequestCultureProvider.UnitTests
{
    [Trait("Category", nameof(UrlLocalizationAwareWebSocketsMiddleware))]
    public class BlazorNegotiateTests
    {
        private readonly HttpContext _context;

        public BlazorNegotiateTests()
        {
            _context = new DefaultHttpContext();
            _context.Request.Path = new PathString("/_blazor/negotiate");
            _context.Request.Method = "POST";
            _context.Request.QueryString = new QueryString($"?negotiateVersion={It.IsAny<string>()}");
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        public async Task When_cluture_is_set_without_trailing_slash(string twoLetterISOLanguageName)
        {
            // Arrange
            _context.Request.Headers["Referer"] = $"http://example.com/{twoLetterISOLanguageName}";

            var root = JsonSerializer.Serialize(new BlazorNegociateBody
            {
                NegotiateVersion = 1,
                ConnectionToken = "0000_XXXX_0000"
            });

            await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(root))
            {
                Position = 0
            };

            Task Next(HttpContext hc)
            {
                hc.Response.ContentType = "application/json";
                hc.Response.Body = stream;

                Asserter();
                return Task.CompletedTask;
            }

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(Next);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            // TODO: Need to assert that the cutlure is in the dictionary

            void Asserter()
            {
                Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        public async Task Ensure_the_response_body_has_been_rewrap(string twoLetterISOLanguageName)
        {
            // Arrange
            _context.Request.Headers["Referer"] = $"http://example.com/{twoLetterISOLanguageName}";

            var root = JsonSerializer.Serialize(new BlazorNegociateBody
            {
                NegotiateVersion = 1,
                ConnectionToken = "0000_XXXX_0000"
            });

            await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(root))
            {
                Position = 0
            };

            Task Next(HttpContext hc)
            {
                hc.Response.ContentType = "application/json";
                hc.Response.Body = stream;

                return Task.CompletedTask;
            }

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(Next);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            Assert.Equal(root, text);
        }
    }
}