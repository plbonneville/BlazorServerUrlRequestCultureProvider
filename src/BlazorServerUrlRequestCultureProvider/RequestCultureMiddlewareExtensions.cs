namespace BlazorServerUrlRequestCultureProvider;

public static class RequestCultureMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLocalizationInteractiveServerRenderMode(this IApplicationBuilder builder, bool useCookie = true)
    {
        return builder
            .UseMiddleware<UrlLocalizationAwareWebSocketsMiddleware>(useCookie);
    }
}