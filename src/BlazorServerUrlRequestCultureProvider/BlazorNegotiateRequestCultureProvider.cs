using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Text.RegularExpressions;
using System.Web;

namespace BlazorServerUrlRequestCultureProvider;

public class BlazorNegotiateRequestCultureProvider : RequestCultureProvider
{
    public BlazorNegotiateRequestCultureProvider(RequestLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
    }

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);     

        // Check if the segment is a blazor segment
        if (!httpContext.Request.Path.StartsWithSegments("/_blazor"))
        {
            return NullProviderCultureResult;
        }

        // Is this a negotiate request or a connect request?
        return httpContext.Request.Method switch
        {
            "POST" => DetermineNegotiateProviderCultureResult(httpContext),
            "CONNECT" => DetermineConnectProviderCultureResult(httpContext),
            _ => NullProviderCultureResult
        };
    }

    private Task<ProviderCultureResult?> DetermineNegotiateProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Request.Path.StartsWithSegments("/_blazor/negotiate"))
        {
            return NullProviderCultureResult;
        }

        var referer = httpContext.Request.Headers.Referer.ToString();
        var uri = new Uri(referer);
        var pathString = new PathString(uri.LocalPath);

        // Get the application base path from the request
        var basePath = httpContext.Request.PathBase;

        // Check if the referer path starts with a supported culture after excluding base path
        foreach (var culture in Options?.SupportedCultures ?? [])
        {
            // Remove the base path before checking for culture
            var pathWithoutBase = pathString.StartsWithSegments(basePath, out var remaining)
                ? remaining.Value
                : pathString.Value;

            if (pathWithoutBase.StartsWith($"/{culture.Name}", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture.Name));
            }
        }

        return NullProviderCultureResult;
    }

    private Task<ProviderCultureResult?> DetermineConnectProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Request.Path.StartsWithSegments("/_blazor/connect"))
        {
            return NullProviderCultureResult;
        }

        var qs = HttpUtility.ParseQueryString(httpContext.Request.QueryString.Value);
        var id = qs["id"];

        if (id is not null
            && UrlLocalizationAwareWebSocketsMiddleware._cultureByConnectionTokens.TryGetValue(id, out var cultureName))
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(cultureName));
        }

        var cookie = UrlLocalizationAwareWebSocketsMiddleware.ConvertCookieToDictionary(httpContext);

        if (cookie.TryGetValue(id, out var cultureCookie))
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(cultureCookie));
        }

        return NullProviderCultureResult;
    }
}
