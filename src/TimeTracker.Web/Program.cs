using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TimeTracker.Web;
using TimeTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Point the HttpClient at the API.
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
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<TimeTrackerApi>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
