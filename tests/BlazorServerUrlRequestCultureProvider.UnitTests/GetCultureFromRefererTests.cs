using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BlazorServerUrlRequestCultureProvider.UnitTests
{
    ////"GET"	{/}
    ////"POST"	{/_blazor/negotiate}	"/_blazor/negotiate?negotiateVersion=1"
    ////"GET"	{/_blazor}				"/_blazor?id=LNjX__7I60EexjX7yLB8zA"

    [Trait("Category", nameof(UrlLocalizationAwareWebSocketsMiddleware))]
    public class GetCultureFromRefererTests
    {
        private readonly HttpContext _context;

        public GetCultureFromRefererTests()
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

            Task Next(HttpContext hc)
            {
                Asserter();
                return Task.CompletedTask;
            }

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(Next);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            void Asserter()
            {
                Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        public async Task When_cluture_is_set_with_trailing_slash(string twoLetterISOLanguageName)
        {
            // Arrange
            _context.Request.Headers["Referer"] = $"http://example.com/{twoLetterISOLanguageName}/";

            Task Next(HttpContext hc)
            {
                Asserter();
                return Task.CompletedTask;
            }

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(Next);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            void Asserter()
            {
                Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            }
        }
    }
}