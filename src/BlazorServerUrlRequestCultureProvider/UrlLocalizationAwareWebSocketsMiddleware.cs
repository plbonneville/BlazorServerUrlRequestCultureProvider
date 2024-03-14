using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using static BlazorServerUrlRequestCultureProvider.UrlLocalizationAwareWebSocketsMiddleware;

namespace BlazorServerUrlRequestCultureProvider;

/// <remarks>
/// Microsoft recommends the use of a cookie to ensures that the WebSocket connection can
/// correctly propagate the culture.
///
///     If localization schemes are based on the URL path or query string, the scheme might not
///     be able to work with WebSockets, thus fail to persist the culture.
///
///     Therefore, use of a localization culture cookie is the recommended approach.
///
/// This is not the approach we want as people can have multiple browser screens (browser tab or
/// iframe) using different languages.
/// </remarks>
public class UrlLocalizationAwareWebSocketsMiddleware
{
    /*
     * Localization Extensibility
     * https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization-extensibility?view=aspnetcore-3.1
     *
     * Write custom ASP.NET Core middleware
     * https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-3.1
     *
     *
     * https://docs.microsoft.com/en-us/aspnet/core/blazor/advanced-scenarios?view=aspnetcore-3.1#blazor-server-circuit-handler
     */

    protected internal static readonly ConcurrentDictionary<string, string> _cultureByConnectionTokens = new ConcurrentDictionary<string, string>();
    private readonly RequestDelegate _next;
    private readonly bool _useCookie;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlLocalizationAwareWebSocketsMiddleware"/> class.
    /// </summary>
    /// <param name="next">
    /// The delegate representing the remaining middleware in the request pipeline.
    /// </param>
    public UrlLocalizationAwareWebSocketsMiddleware(RequestDelegate next, bool useCookie = true)
    {
        _next = next;
        _useCookie = useCookie;
    }

    /// <summary>
    /// Handles the requests and returns a Task that represents the execution of the middleware.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context reprensenting the request.
    /// </param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var segments = httpContext.Request.Path.Value!
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var nextAction = segments switch
        {
            string[] { Length: 2 } x
                when x[0] == "_blazor" && x[1] == "negotiate"
                && httpContext.Request.Method == "POST"
                => BlazorNegotiate,

            // Blazor 8 Interactive Server
            string[] { Length: 1 } x
                when x[0] == "_blazor"
                && httpContext.Request.QueryString.HasValue
                && httpContext.Request.Method == "CONNECT"
                => BlazorConnect,

            _ => _next
        };

        await nextAction(httpContext);
    }

    /// <summary>
    /// On blazor connect, set the culture based on the dictionary entry created in
    /// <see cref="BlazorNegotiate(HttpContext httpContext)"/>.
    /// </summary>
    private async Task BlazorConnect(HttpContext httpContext)
    {
        var components = QueryHelpers.ParseQuery(httpContext.Request.QueryString.Value);
        string? connectionToken = components["id"];

        if (string.IsNullOrEmpty(connectionToken))
        {
            throw new InvalidOperationException("The connectionToken must be passed in the query string");
        }

        // Set the culture based on the cookie entry created in BlazorNegotiate
        if (_useCookie)
        {
            var cookie = ConvertCookieToDictionary(httpContext);

            if (cookie.TryGetValue(connectionToken, out var currentCulture))
            {
                var culture = new CultureInfo(currentCulture);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }
        // Set the culture based on the dictionary entry created in BlazorNegotiate
        else
        {
            var currentCulture = _cultureByConnectionTokens[connectionToken!];

            var culture = new CultureInfo(currentCulture);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        await _next(httpContext);

        // If using the dictionary, clean-up the memory by removing the token
        // from the dictionary. Nothing we can do if using the cookie, as the
        // cookie is already sent to the browser. The cookie should be removed
        // by the browser when the session ends.
        if (!_useCookie && httpContext.Response.StatusCode == StatusCodes.Status200OK)
        {
            // When "closing" the SignalR connection (websocket) clean-up the memory by removing the
            // token from the dictionary.
            _cultureByConnectionTokens.TryRemove(connectionToken, out var _);
        }
    }

    /// <summary>
    /// On blazor negotiate, save the culture into a dictionary.
    /// <see cref="BlazorHeartbeat(HttpContext)"/>.
    /// </summary>
    private async Task BlazorNegotiate(HttpContext httpContext)
    {
        // Enable the rewinding of the body after the action has been called
        httpContext.Request.EnableBuffering();

        // Save the reference of the response body
        var originalResponseBodyStream = httpContext.Response.Body;
        using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        await _next(httpContext);

        // Temporary unwarp the response body to get the connectionToken
        var responseBodyContent = await ReadResponseBodyAsync(httpContext.Response);

        if (httpContext.Response.ContentType == "application/json")
        {
            var root = JsonSerializer.Deserialize<BlazorNegotiateBody>(responseBodyContent);

            if (root?.ConnectionToken is null)
            {
                throw new InvalidOperationException("ConnectionToken is null");
            }

            if (_useCookie)
            {
                var cookie = ConvertCookieToDictionary(httpContext);

                cookie[root.ConnectionToken] = CultureInfo.CurrentCulture.Name;
                var message = JsonSerializer.Serialize(cookie.ToArray());
                httpContext.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(httpContext));
            }
            else
            {
                if (!_cultureByConnectionTokens.TryAdd(root.ConnectionToken, CultureInfo.CurrentCulture.Name))
                {
                    _cultureByConnectionTokens[root.ConnectionToken] = CultureInfo.CurrentCulture.Name;
                }
            }
        }

        // Rewind the response body as if we hadn't upwrap-it
        await responseBody.CopyToAsync(originalResponseBodyStream);

        static async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, leaveOpen: true);
            var bodyAsText = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }
    }

    internal static IDictionary<string, string> ConvertCookieToDictionary(HttpContext httpContext)
    {
        var cookieContent = new Dictionary<string, string>();

        var cookie = httpContext.Request.Cookies[StatusCookieName];

        if (cookie is null)
        {
            return cookieContent;
        }

        var kvps = JsonSerializer.Deserialize<KeyValuePair<string, string>[]>(cookie);

        foreach (var item in kvps)
        {
            cookieContent[item.Key] = item.Value;
        }

        return cookieContent;
    }

    internal static string MakeCookieValue(IDictionary<string, string> cookieContent)
    {
        // var kvps = cookieContent.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToArray();

        return JsonSerializer.Serialize(cookieContent.ToArray());
    }

    public const string StatusCookieName = ".AspNetCore.CircuitCulture";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(60),
    };
}