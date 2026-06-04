using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TimeTracker.Web;
using TimeTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Point the HttpClient at the API (configurable via wwwroot/appsettings.json).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5130";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<TimeTrackerApi>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
