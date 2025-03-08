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

        // Get the path excluding the base path
        var pathWithoutBase = httpContext.Request.Path.Value!;
        if (!string.IsNullOrEmpty(httpContext.Request.PathBase.Value))
        {
            // If there's a base path, ensure we're only looking at the part after it
            if (pathWithoutBase.StartsWith(httpContext.Request.PathBase.Value, StringComparison.OrdinalIgnoreCase))
            {
                pathWithoutBase = pathWithoutBase[httpContext.Request.PathBase.Value.Length..];
            }
        }

        // Check if the remaining path starts with a supported culture
        foreach (var culture in Options?.SupportedCultures ?? [])
        {
            if (pathWithoutBase.StartsWith($"/{culture.Name}", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture.Name));
            }
        }

        return NullProviderCultureResult;
    }
}
