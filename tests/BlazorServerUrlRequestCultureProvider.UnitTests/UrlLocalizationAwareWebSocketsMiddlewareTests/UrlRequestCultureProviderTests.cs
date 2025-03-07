using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace BlazorServerUrlRequestCultureProvider.UnitTests;

public partial class UrlRequestCultureProviderTests
{
    private readonly RequestLocalizationOptions _options;
    private readonly UrlRequestCultureProvider _provider;

    public UrlRequestCultureProviderTests()
    {
        _options = new RequestLocalizationOptions
        {
            SupportedCultures = [new("en"), new("fr")],
            SupportedUICultures = [new("en"), new("fr")],
            DefaultRequestCulture = new RequestCulture("en")
        };

        _provider = new UrlRequestCultureProvider(_options);
    }

    [Theory]
    [InlineData("/fr/accueil "     , null, "fr")]
    [InlineData("/en/about"        , null, "en")]
    [InlineData("/myapp/fr/accueil", "/myapp", "fr")]
    [InlineData("/myapp/en/about"  , "/myapp", "en")]
    [InlineData("/es/inicio"       , null, null)] // Unsupported culture
    [InlineData("/about"           , null, null)] // No culture specified
    public async Task DetermineProviderCultureResult_ReturnsExpectedCulture(string path, string? pathBase, string? expectedCulture)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (pathBase != null)
        {
            context.Request.PathBase = pathBase;
        }

        // Act
        var result = await _provider.DetermineProviderCultureResult(context);

        // Assert
        if (expectedCulture is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Single(result.Cultures);
            Assert.Equal(expectedCulture, result.Cultures[0]);
        }
    }

    [Fact]
    public async Task DetermineProviderCultureResult_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _provider.DetermineProviderCultureResult(null!));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UrlRequestCultureProvider(null!));
    }

    [Theory]
    [InlineData("/FR/accueil")] // Test case sensitivity
    [InlineData("/fr/")]        // Test trailing slash
    [InlineData("/fr")]         // Test without trailing path
    public async Task DetermineProviderCultureResult_HandlesVariousValidPaths(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        // Act
        var result = await _provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Cultures);
        Assert.Equal("fr", result.Cultures[0], ignoreCase: true);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task GetCultureInfoFromFristSegment(string twoLetterISOLanguageName)
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
                    options.RequestCultureProviders.Insert(0, new UrlRequestCultureProvider(options));

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
            var response = await client.GetAsync($"/{twoLetterISOLanguageName}/page");
        }
    }

    [Fact]
    public async Task GetDefaultCultureInfoIfCultureSegmentIsMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en")
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("en", requestCulture.Culture.Name);
                        Assert.Equal("en", requestCulture.UICulture.Name);
                        return Task.CompletedTask;
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page");
        }
    }
}