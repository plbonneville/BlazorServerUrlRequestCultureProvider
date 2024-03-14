using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BlazorServerUrlRequestCultureProvider.UnitTests;

[Trait("Category", nameof(UrlLocalizationAwareWebSocketsMiddleware))]
public class BlazorConnectTests
{
    //public const string Id = "0000_XXXX_0000";
    public readonly string Id = Guid.NewGuid().ToString();

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task When_culture_is_in_the_dictionary(string twoLetterISOLanguageName)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: false);

                    app.Run(context =>
                    {
                        Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.Name);
                        Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentUICulture.Name);

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            FakeUrlLocalizationAwareWebSocketsMiddleware
                .CultureByConnectionTokens.TryAdd(Id, twoLetterISOLanguageName);

            var message = new HttpRequestMessage(HttpMethod.Connect, $"/_blazor?id={Id}");

            var response = await client.SendAsync(message);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task When_status_code_is_OK_cleanup_cache(string twoLetterISOLanguageName)
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: false);

                    app.Run(context =>
                    {
                        context.Response.StatusCode = 200;

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            FakeUrlLocalizationAwareWebSocketsMiddleware
                .CultureByConnectionTokens.TryAdd(Id, twoLetterISOLanguageName);

            var message = new HttpRequestMessage(HttpMethod.Connect, $"/_blazor?id={Id}");

            // Act
            var response = await client.SendAsync(message);

            var canGetValue = FakeUrlLocalizationAwareWebSocketsMiddleware
                .CultureByConnectionTokens.TryGetValue(Id, out var culture);

            // Assert
            Assert.False(canGetValue);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task When_culture_is_in_the_cookie(string twoLetterISOLanguageName)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: true);

                    app.Run(context =>
                    {
                        Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentCulture.Name);
                        Assert.Equal(twoLetterISOLanguageName, CultureInfo.CurrentUICulture.Name);

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Connect, $"/_blazor?id={Id}");

            IDictionary<string, string> cookie = new Dictionary<string, string>
            {
                [Id] = twoLetterISOLanguageName
            };

            var cookieContent = JsonSerializer.Serialize(cookie.ToArray());
            var encoded = HttpUtility.UrlEncode(cookieContent);

            var cookieValue = new CookieHeaderValue(UrlLocalizationAwareWebSocketsMiddleware.StatusCookieName, encoded)
                .ToString();

            client.DefaultRequestHeaders.Add("Cookie", cookieValue);

            var response = await client.SendAsync(message);
        }
    }
}
