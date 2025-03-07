# BlazorServerUrlRequestCultureProvider

Localization scheme based on the URL path that works with Blazor Server (WebSockets) and Blazor Static SSR.

## How it works

1. Set the culture on the first HTTP request
2. On blazor negotiate:
    - Check the websocket request referrer; in our case `/{TWO_LETTER_ISO_LANGUAGE_NAME}/{SOME_URI}`
    - Change the culture base on the websocket request referrer
    - Store the connection token and culture key/value pair
3. On Blazor heartbeat:
    - Using the connection token, retrieve the stored culture
    - Change the culture based on the stored value
4. When closing the websocket:
    Remove the stored connection token and culture key/value pair

## How to use

Add the NuGet package to your Blazor Server project:

```bash
dotnet add package BlazorServerUrlRequestCultureProvider
```

In your `Program.cs` file, add the following code:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Remove the default providers
    // 1. QueryStringRequestCultureProvider
    // 2. CookieRequestCultureProvider
    // 3. AcceptLanguageHeaderRequestCultureProvider
    options.RequestCultureProviders.Clear();

    IList<CultureInfo> supportedCultures = [new("en"), new("fr")];

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.ApplyCurrentCultureToResponseHeaders = true;

    // Configure globalization for static server rendering (SSR)
    options.RequestCultureProviders.Insert(0, new UrlRequestCultureProvider(options));

    // Configure globalization for interactive server rendering using Blazor Server
    options.RequestCultureProviders.Insert(1, new BlazorNegotiateRequestCultureProvider(options));
});

// ...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRequestLocalization();
// app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: false); // Server-side ConcurrentDictionary storage
app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: true); // Client-side cookie storage

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```
