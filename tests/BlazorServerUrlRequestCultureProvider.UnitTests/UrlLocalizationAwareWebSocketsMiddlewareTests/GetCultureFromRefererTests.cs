using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public GetCultureFromRefererTests()
        {
            _context = new DefaultHttpContext();
            _context.Request.Path = new PathString("/_blazor/negotiate");
            _context.Request.Method = "POST";
            _context.Request.QueryString = new QueryString($"?negotiateVersion={It.IsAny<string>()}");

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(_context);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        public async Task When_cluture_is_set_without_trailing_slash(string twoLetterISOLanguageName)
        {
            // Arrange
            _context.Request.Headers["Referer"] = $"http://example.com/{twoLetterISOLanguageName}";

            RequestDelegate next = (HttpContext hc) =>
            {
                Asserter();
                return Task.CompletedTask;
            };

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(next);

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

            RequestDelegate next = (HttpContext hc) =>
            {
                Asserter();
                return Task.CompletedTask;
            };

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(next);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            void Asserter()
            {
                Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            }
        }

        [Theory]
        [InlineData("en", "/app1")]
        [InlineData("fr", "/app1")]
        public async Task When_cluture_is_set_with_path_base(string twoLetterISOLanguageName, string pathBase)
        {
            // Arrange
            _context.Request.Headers["Referer"] = $"http://example.com{pathBase}/{twoLetterISOLanguageName}/";
            _context.Request.PathBase = pathBase;

            RequestDelegate next = (HttpContext hc) =>
            {
                Asserter();
                return Task.CompletedTask;
            };

            var sutMiddleware = new UrlLocalizationAwareWebSocketsMiddleware(next);

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
