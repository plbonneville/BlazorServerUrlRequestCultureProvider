using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;

namespace BlazorServerUrlRequestCultureProvider
{
    public static class HttpContextExtensions
    {
        public static string GetCultureFromRequest(this HttpContext httpContext)
            => GetCultureFromPath(httpContext.Request.Path.Value, httpContext.Request.PathBase.Value);
        

        public static string GetCultureFromReferer(this HttpContext httpContext)
        {
            var referer = httpContext.Request.Headers["Referer"].ToString();
            var uri = new Uri(referer);

            return GetCultureFromPath(uri.LocalPath, httpContext.Request.PathBase.Value);
        }

        private static string GetCultureFromPath(string path, string pathBase)
        {
            if (!string.IsNullOrEmpty(pathBase) && path.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(pathBase.Length);
            }

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 1 && segments[0].Length == 2)
            {
                var currentCulture = segments[0];
                return currentCulture;
            }

            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }
    }
}