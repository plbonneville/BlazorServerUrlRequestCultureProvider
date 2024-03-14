# URL based localization scheme for Blazor 8

## Introduction

Blazor 8 now has four render modes.

Name | Description | Render location | Interactive
---- | ----------- | :-------------: | :---------:
Static | Static server rendering |  Server  | <span aria-hidden="true">❌</span><span class="visually-hidden">No</span>
Interactive Server | Interactive server rendering using Blazor Server | Server | <span aria-hidden="true">✔️</span><span class="visually-hidden">Yes</span>
Interactive WebAssembly | Interactive client rendering using Blazor WebAssembly | Client | <span aria-hidden="true">✔️</span><span class="visually-hidden">Yes</span>
Interactive Auto | Interactive client rendering using Blazor Server initially and then WebAssembly on subsequent visits after the Blazor bundle is downloaded | Server, then client | <span aria-hidden="true">✔️</span><span class="visually-hidden">Yes</span>

Lets look at URL based localization scheme for each of these render modes and how they work together.

## Static

1. Create a new `IRequestCultureProvider` that will determine the culture for the request based on the URL. The following will check if the URL starts with the culture name and if so, return that culture.

    ```csharp
    using Microsoft.AspNetCore.Localization;
    using System.Globalization;

    /// <summary>
    /// Represents a provider that determines the culture for a request based on the URL.
    /// </summary>
    public class UrlRequestCultureProvider : IRequestCultureProvider
    {
        private readonly List<CultureInfo> _supportedCultures;

        public UrlRequestCultureProvider(List<CultureInfo> supportedCultures)
        {
            _supportedCultures = supportedCultures;
        }

        /// <summary>
        /// Determines the culture for the request based on the first segment of the URL.
        /// </summary>
        /// <param name="httpContext">The HttpContext representing the current request.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result
        /// contains the ProviderCultureResult representing the determined culture.
        /// </returns>
        public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            // Loop through the supported cultures and check if the URL starts with
            // the culture name
            foreach (var culture in _supportedCultures)
            {
                if (httpContext.Request.Path.StartsWithSegments($"/{culture.Name}"))
                {
                    return Task.FromResult<ProviderCultureResult?>(new(culture.Name));
                }
            }

            return Task.FromResult<ProviderCultureResult?>(new(CultureInfo.CurrentCulture.Name));
        }
    }
    ```

2. Register the localization services with `AddLocalization` extension.

    ```csharp
    builder.Services.AddLocalization();
    ```

3. Then register the `RequestLocalizationOptions` with the our `UrlRequestCultureProvider` as the provider.

    ```csharp
    // Configure globalization for Server-Side-Rendering (SSR)
    builder.Services.Configure<RequestLocalizationOptions>(opt =>
    {
        IList<CultureInfo> supportedCultures = [new("en"), new("fr")];

        opt.DefaultRequestCulture = new RequestCulture("en");
        opt.SupportedCultures = supportedCultures;
        opt.SupportedUICultures = supportedCultures;

        opt.RequestCultureProviders.Insert(0, new UrlRequestCultureProvider(supportedCultures));
    });
    ```

4. Lastly, add the `RequestLocalizationMiddleware` to automatically set culture information for requests based on the URL.

    ```csharp
    app.UseRequestLocalization();
    ```

## Interactive Server

1. Follow the same steps as the Static render mode.
2. Add the `UrlLocalizationAwareWebSocketsMiddleware` to automatically set the culture for Interactive Server requests based on the URL.

    ```csharp
    app.UseMiddleware<UrlLocalizationAwareWebSocketsMiddleware>();
    app.UseRequestLocalization();
    ```

## Interactive WebAssembly

1. Follow the same steps as the Static render mode.
2. Set the `BlazorWebAssemblyLoadAllGlobalizationData`` property to true in the app's project file (`.csproj`):

    ```xml
    <PropertyGroup>
        <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
    </PropertyGroup>
    ```
3. Prevent Blazor autostart by adding autostart="false" to Blazor's script tag:

    ```html
    <script src="_framework/blazor.web.js" autostart="false"></script>
    ```
4. Add the following `<script>` block after Blazor's `<script>` tag and before the closing `</body>` tag:

    ```html
    <script>
        Blazor.start({
            webAssembly: {
                applicationCulture: '@System.Globalization.CultureInfo.CurrentCulture.Name'
            }
        });
        </script>
    ```

## Interactive Auto

1. Follow the same steps as the Interactive Server render mode.
2. Follow the same steps as the Interactive WebAssembly render mode.