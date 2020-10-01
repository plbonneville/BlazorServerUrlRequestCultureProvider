using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using System.Threading.Tasks;

namespace BlazorServerUrlRequestCultureProvider
{
    public static class RequestCultureMiddlewareExtensions
    {
        [UsedImplicitly]
        public static IApplicationBuilder UseUrlRequestLocalization(this IApplicationBuilder builder, RequestLocalizationOptions options)
        {
            options.RequestCultureProviders.Clear();

            options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
            {
                var currentCulture = context.GetCultureFromRequest();

                var requestCulture = new ProviderCultureResult(currentCulture, currentCulture);

                return await Task.FromResult(requestCulture);
            }));

            return builder
                .UseMiddleware<UrlLocalizationAwareWebSocketsMiddleware>()
                .UseRequestLocalization(options);
        }
    }
}
