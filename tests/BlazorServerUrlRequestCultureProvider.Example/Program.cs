using BlazorServerUrlRequestCultureProvider;
using BlazorServerUrlRequestCultureProvider.Example;
using BlazorServerUrlRequestCultureProvider.Example.Components;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
//builder.Services.AddLocalization();

builder.Services.AddLTDLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // https://gist.github.com/vaclavholusa-LTD/2a27d0bb0af5c07589cffbf1c2fff4f4

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

    //// Configure globalization for interactive server rendering using Blazor Server
    //options.RequestCultureProviders.Insert(2, new BlazorConnectRequestCultureProvider(options));
});

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
app.UseRequestLocalizationInteractiveServerRenderMode(useCookie: true);

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
