using Microsoft.Extensions.Logging;
using TimeTracker.Mobile.Pages;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        // No custom fonts bundled — the app uses each platform's system font, which keeps
        // the project free of binary TTF assets. Add ConfigureFonts + Resources/Fonts/*.ttf
        // later if a branded typeface is wanted.
        builder.UseMauiApp<App>();

        // --- Services ---
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddTransient<AuthHttpMessageHandler>();

        // Offline cache + sync. All singletons: one SQLite connection + one sync gate for
        // the app's lifetime.
        builder.Services.AddSingleton<TimeTracker.Mobile.Data.LocalStore>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<DataService>();

        // API HttpClient with the bearer-token handler attached.
        builder.Services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            })
            .AddHttpMessageHandler<AuthHttpMessageHandler>();

        // --- Pages (DI-resolved so they get the services above) ---
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LogTimePage>();
        builder.Services.AddTransient<AddTimeEntryPage>();
        builder.Services.AddTransient<TasksPage>();
        builder.Services.AddTransient<EditTaskPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
