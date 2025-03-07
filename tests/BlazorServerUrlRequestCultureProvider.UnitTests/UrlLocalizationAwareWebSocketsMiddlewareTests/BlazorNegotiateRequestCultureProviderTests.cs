using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace BlazorServerUrlRequestCultureProvider.UnitTests;

public class BlazorNegotiateRequestCultureProviderTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task GetCultureInfoFromRefererFirstSegment(string twoLetterISOLanguageName)
    {
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
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;

                        Assert.Equal(twoLetterISOLanguageName, requestCulture.Culture.Name);
                        Assert.Equal(twoLetterISOLanguageName, requestCulture.UICulture.Name);

                        return Task.CompletedTask;
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            client.DefaultRequestHeaders.Referrer = new Uri($"https://example.com/{twoLetterISOLanguageName}/page");

            var response = await client.PostAsJsonAsync("/_blazor/negotiate", "");
        }
    }

    [Fact]
    public async Task DetermineProviderCultureResult_NotBlazorPath_ReturnsNull()
    {
        // Arrange
        var options = new RequestLocalizationOptions();
        var provider = new BlazorNegotiateRequestCultureProvider(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/something";

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DetermineProviderCultureResult_BlazorNegotiateWithValidCulture_ReturnsCulture()
    {
        // Arrange
        var options = new RequestLocalizationOptions
        {
            SupportedCultures = [new("en"), new("fr")]
        };
        var provider = new BlazorNegotiateRequestCultureProvider(options);
        var context = new DefaultHttpContext();

        context.Request.Path = "/_blazor/negotiate";
        context.Request.Method = "POST";
        context.Request.Headers.Referer = "https://example.com/en/some/path";

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("en", result.Cultures.FirstOrDefault().Value);
    }

    [Fact]
    public async Task DetermineProviderCultureResult_BlazorNegotiateWithBasePathAndValidCulture_ReturnsCulture()
    {
        // Arrange
        var options = new RequestLocalizationOptions
        {
            SupportedCultures = [new("en"), new("fr")]
        };
        var provider = new BlazorNegotiateRequestCultureProvider(options);
        var context = new DefaultHttpContext();

        context.Request.Path = "/_blazor/negotiate";
        context.Request.Method = "POST";
        context.Request.PathBase = "/myapp";
        context.Request.Headers.Referer = "https://example.com/myapp/fr/some/path";

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fr", result.Cultures.FirstOrDefault().Value);
    }

    [Fact]
    public async Task DetermineProviderCultureResult_BlazorNegotiateWithUnsupportedCulture_ReturnsNull()
    {
        // Arrange
        var options = new RequestLocalizationOptions
        {
            SupportedCultures = [new("en"), new("fr")]
        };
        var provider = new BlazorNegotiateRequestCultureProvider(options);
        var context = new DefaultHttpContext();

        context.Request.Path = "/_blazor/negotiate";
        context.Request.Method = "POST";
        context.Request.Headers.Referer = "https://example.com/es/some/path";

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task DetermineProviderCultureResult_NonPostMethod_ReturnsNull(string method)
    {
        // Arrange
        var options = new RequestLocalizationOptions();
        var provider = new BlazorNegotiateRequestCultureProvider(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_blazor/negotiate";
        context.Request.Method = method;

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.Null(result);
    }
}