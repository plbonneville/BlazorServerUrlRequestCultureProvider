using Microsoft.AspNetCore.Http;
using Moq;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace BlazorServerUrlRequestCultureProvider.UnitTests
{
    public class BlazorHeartbeatTests
    {
        public const string Id = "0000_XXXX_0000";
        private readonly HttpContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public BlazorHeartbeatTests()
        {
            _context = new DefaultHttpContext();
            _context.Request.Path = new PathString("/_blazor");
            _context.Request.Method = "GET";
            _context.Request.QueryString = new QueryString($"?id={Id}");

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(_context);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        [Trait("Category", nameof(UrlLocalizationAwareWebSocketsMiddleware))]
        public async Task When_culture_is_in_the_dictionary(string twoLetterISOLanguageName)
        {
            Task next(HttpContext hc)
            {
                Asserter();
                return Task.CompletedTask;
            }

            var sutMiddleware = new FakeUrlLocalizationAwareWebSocketsMiddleware(next, twoLetterISOLanguageName);

            // Act
            await sutMiddleware.InvokeAsync(_context);

            // Assert
            void Asserter()
            {
                Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            }
        }
    }

    public class FakeUrlLocalizationAwareWebSocketsMiddleware : UrlLocalizationAwareWebSocketsMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UrlLocalizationAwareWebSocketsMiddleware"/> class.
        /// </summary>
        /// <param name="next">
        /// The delegate representing the remaining middleware in the request pipeline.
        /// </param>
        public FakeUrlLocalizationAwareWebSocketsMiddleware(RequestDelegate next, string twoLetterISOLanguageName)
            : base(next)
        {
            CultureByConnectionTokens[BlazorHeartbeatTests.Id] = twoLetterISOLanguageName;
        }
    }
}
