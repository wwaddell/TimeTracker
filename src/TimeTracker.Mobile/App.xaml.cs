using TimeTracker.Mobile.Pages;

namespace TimeTracker.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start on the login page; LoginPage routes into the AppShell once a token is in
        // hand (or immediately if a cached account can sign in silently).
        var login = _services.GetRequiredService<LoginPage>();
        return new Window(new NavigationPage(login));
    }
}
