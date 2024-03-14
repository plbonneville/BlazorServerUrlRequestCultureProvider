using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Xunit;

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
                        
                        return Task.FromResult(0);
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
}
