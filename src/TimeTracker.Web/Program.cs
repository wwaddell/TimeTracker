using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TimeTracker.Web;
using TimeTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Point the HttpClient at the API (configurable via wwwroot/appsettings.json).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5130";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<TimeTrackerApi>();

await builder.Build().RunAsync();
