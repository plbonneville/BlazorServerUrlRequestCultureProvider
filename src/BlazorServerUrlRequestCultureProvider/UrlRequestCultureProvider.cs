using Microsoft.AspNetCore.Localization;

namespace BlazorServerUrlRequestCultureProvider;

/// <summary>
/// Represents a provider that determines the culture for a request based on the URL.
/// </summary>
public class UrlRequestCultureProvider : RequestCultureProvider
{
    public UrlRequestCultureProvider(RequestLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
    }

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Check if the path starts with a supported culture
        foreach (var culture in Options?.SupportedCultures ?? [])
        {
            if (httpContext.Request.Path.StartsWithSegments($"/{culture.Name}"))
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture.Name));
            }
        }

        return NullProviderCultureResult;
    }
}
