using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace BlazorServerUrlRequestCultureProvider.UnitTests;

public class FakeUrlLocalizationAwareWebSocketsMiddleware(RequestDelegate next)
    : UrlLocalizationAwareWebSocketsMiddleware(next)
{
    // Expose the dictionary so we can use it in the tests
    internal static ConcurrentDictionary<string, string> CultureByConnectionTokens
        => _cultureByConnectionTokens;
}
