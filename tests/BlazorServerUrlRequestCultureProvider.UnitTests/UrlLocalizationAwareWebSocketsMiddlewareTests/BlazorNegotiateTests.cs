using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace BlazorServerUrlRequestCultureProvider.UnitTests;

[Trait("Category", nameof(UrlLocalizationAwareWebSocketsMiddleware))]
public class BlazorNegotiateTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task GetCultureInfoFromRefererFirstSegment(string twoLetterISOLanguageName)
    {
        var body = new BlazorNegociateBody
        {
            negotiateVersion = 1,
            //connectionToken = "0000_XXXX_0000"
            connectionToken = Guid.NewGuid().ToString(),
        };

        var root = JsonSerializer.Serialize(body);
        byte[] array = Encoding.ASCII.GetBytes(root);
        using var responseBody = new MemoryStream(array);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    IList<CultureInfo> supportedCultures = [new("en"), new("fr")];

                    var options = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en"),
                        SupportedCultures = supportedCultures,
                        SupportedUICultures = supportedCultures
                    };

                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Insert(0, new BlazorNegotiateRequestCultureProvider(options));

                    app.UseRequestLocalization(options);
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: false);

                    app.Run(context =>
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.Body = responseBody;

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            client.DefaultRequestHeaders.Referrer = new Uri($"https://example.com/{twoLetterISOLanguageName}/page");

            var response = await client.PostAsJsonAsync("/_blazor/negotiate", string.Empty);

            FakeUrlLocalizationAwareWebSocketsMiddleware
                .CultureByConnectionTokens.TryGetValue(body.connectionToken, out var value);

            Assert.Equal(twoLetterISOLanguageName, value);
        }
    }

    [Fact]
    public async Task Ensure_the_response_body_has_been_rewrap()
    {
        // Arrange
        var body = new BlazorNegociateBody
        {
            negotiateVersion = 1,
            //connectionToken = "0000_XXXX_0000"
            connectionToken = Guid.NewGuid().ToString(),
        };

        var root = JsonSerializer.Serialize(body);
        byte[] array = Encoding.ASCII.GetBytes(root);
        using var responseBody = new MemoryStream(array);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalizationInteractiveServerRenderMode();

                    app.Run(context =>
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.Body = responseBody;

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/_blazor/negotiate", string.Empty);

            // Assert
            using var reader = new StreamReader(responseBody);
            string text = reader.ReadToEnd();
            Assert.Equal(root, text);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task SetsCookie(string twoLetterISOLanguageName)
    {
        var body = new BlazorNegociateBody
        {
            negotiateVersion = 1,
            //connectionToken = "0000_XXXX_0000"
            connectionToken = Guid.NewGuid().ToString(),
        };

        var root = JsonSerializer.Serialize(body);
        byte[] array = Encoding.ASCII.GetBytes(root);
        using var responseBody = new MemoryStream(array);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    IList<CultureInfo> supportedCultures = [new("en"), new("fr")];

                    var options = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en"),
                        SupportedCultures = supportedCultures,
                        SupportedUICultures = supportedCultures
                    };

                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Insert(0, new BlazorNegotiateRequestCultureProvider(options));

                    app.UseRequestLocalization(options);
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: true);

                    app.Run(context =>
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.Body = responseBody;

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            client.DefaultRequestHeaders.Referrer = new Uri($"https://example.com/{twoLetterISOLanguageName}/page");

            HttpResponseMessage response = await client.PostAsJsonAsync("/_blazor/negotiate", string.Empty);

            IEnumerable<string> cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

            var decoded = HttpUtility.UrlDecode(cookies.First());
            Assert.Contains(UrlLocalizationAwareWebSocketsMiddleware.StatusCookieName, decoded);
            Assert.Contains(body.connectionToken, decoded);
        }
    }

    [Theory]
    [InlineData("en", "fr")]
    [InlineData("fr", "en")]
    public async Task AddValueToExistingCookie(string twoLetterISOLanguageName1, string twoLetterISOLanguageName2)
    {
        var body1 = new BlazorNegociateBody
        {
            negotiateVersion = 1,
            //connectionToken = "0000_XXXX_0000"
            connectionToken = Guid.NewGuid().ToString(),
        };

        var body2 = new BlazorNegociateBody
        {
            negotiateVersion = 1,
            //connectionToken = "0000_XXXX_0000"
            connectionToken = Guid.NewGuid().ToString(),
        };

        var root = JsonSerializer.Serialize(body2);
        byte[] array = Encoding.ASCII.GetBytes(root);
        using var responseBody = new MemoryStream(array);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    IList<CultureInfo> supportedCultures = [new("en"), new("fr")];

                    var options = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en"),
                        SupportedCultures = supportedCultures,
                        SupportedUICultures = supportedCultures
                    };

                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Insert(0, new BlazorNegotiateRequestCultureProvider(options));

                    app.UseRequestLocalization(options);
                    app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: true);

                    app.Run(context =>
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.Body = responseBody;

                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            {
                client.DefaultRequestHeaders.Referrer = new Uri($"https://example.com/{twoLetterISOLanguageName2}/page");


                IDictionary<string, string> cookie = new Dictionary<string, string>
                {
                    [body1.connectionToken] = twoLetterISOLanguageName1
                };

                var cookieContent = JsonSerializer.Serialize(cookie.ToArray());
                var encoded = HttpUtility.UrlEncode(cookieContent);

                var cookieValue = new CookieHeaderValue(UrlLocalizationAwareWebSocketsMiddleware.StatusCookieName, encoded)
                    .ToString();

                client.DefaultRequestHeaders.Add("Cookie", cookieValue);
            }

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/_blazor/negotiate", string.Empty);

            // Assert
            {
                // Get the cookie
                var cookie = response.Headers
                    .SingleOrDefault(header => header.Key == "Set-Cookie").Value
                    .Single();

                // Decode the cookie
                var decoded = HttpUtility.UrlDecode(cookie);

                // Extract the cookie value
                var cookieSerialized = Regex.Match(decoded, $"(?<={UrlLocalizationAwareWebSocketsMiddleware.StatusCookieName}=)(.*?)(?=;)");

                // Deserialize the cookie value
                var kvps = JsonSerializer.Deserialize<KeyValuePair<string, string>[]>(cookieSerialized.Value);

                Assert.Equal(2, kvps.Length);

                Assert.Equal(body1.connectionToken, kvps[0].Key);
                Assert.Equal(twoLetterISOLanguageName1, kvps[0].Value);

                Assert.Equal(body2.connectionToken, kvps[1].Key);
                Assert.Equal(twoLetterISOLanguageName2, kvps[1].Value);
            }
        }
    }

    private class BlazorNegociateBody
    {
        public int negotiateVersion { get; set; }
        public string connectionToken { get; set; }
    }
}
