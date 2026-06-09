using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TimeTracker.Web;
using TimeTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- API base address ---
//   - Hosted (prod, and dev when running through the API at :5130): the API serves both
//     the WASM client and the JSON endpoints from the same origin, so the right base is
//     whatever served us — HostEnvironment.BaseAddress.
//   - Standalone WASM dev (Blazor DevServer at :5008): cross-origin to the API at :5130;
//     wwwroot/appsettings.Development.json sets ApiBaseUrl to point at it. That file is
//     only loaded when the WASM host environment is Development (signaled by the
//     Blazor-Environment header in hosted runs, or by the DevServer in standalone).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = builder.HostEnvironment.BaseAddress;
}

// --- MSAL (Entra External ID) ---
// Config is read from wwwroot/appsettings.json under "AzureAd". When AzureAd:ClientId is
// blank we skip MSAL registration entirely so the app boots in dev environments that
// haven't set it up yet — the dev backdoor on the API still authenticates requests in
// that case. In prod, missing AzureAd config + Auth__UseDevBypass=false would mean
// nobody can log in; either configure MSAL here or flip the bypass on temporarily.
var hasMsalConfig = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]);
if (hasMsalConfig)
{
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        // The API scope: a request for this scope on every silent token acquisition makes
        // sure the access token's `aud` matches our API's audience (the API client ID URI).
        // Without a default scope MSAL returns only an ID token, which the API can't use.
        var apiScope = builder.Configuration["AzureAd:DefaultScope"];
        if (!string.IsNullOrWhiteSpace(apiScope))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
        }
    });
}

// --- API HttpClient ---
// When MSAL is configured, AuthorizationMessageHandler attaches a Bearer token to every
// request to the API (matched by base address). Otherwise plain HttpClient — relies on
// the API's dev backdoor to authenticate.
if (hasMsalConfig)
{
    builder.Services.AddScoped<BaseAddressAuthorizationMessageHandler>();
    builder.Services.AddHttpClient("TimeTrackerApi", client => client.BaseAddress = new Uri(apiBaseUrl))
        .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
    builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("TimeTrackerApi"));
}
else
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
}

builder.Services.AddScoped<TimeTrackerApi>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
