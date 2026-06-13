using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly ApiClient _api;
    private readonly AppState _state;
    private bool _triedSilent;

    public LoginPage(AuthService auth, ApiClient api, AppState state)
    {
        InitializeComponent();
        _auth = auth;
        _api = api;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Auto-advance if a cached account can sign in silently — so returning users skip
        // the button. Only attempt once per page lifetime.
        if (_triedSilent)
        {
            return;
        }
        _triedSilent = true;

        if (await _auth.IsSignedInAsync())
        {
            await CompleteSignInAsync();
        }
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var token = await _auth.GetAccessTokenAsync(allowInteractive: true);
            if (string.IsNullOrEmpty(token))
            {
                // User cancelled the browser prompt.
                SetBusy(false);
                return;
            }
            await CompleteSignInAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Sign-in failed: {ex.Message}");
            SetBusy(false);
        }
    }

    // Load the profile + orgs into shared state, then swap the root to the tab shell.
    private async Task CompleteSignInAsync()
    {
        SetBusy(true);
        try
        {
            _state.Me = await _api.GetMeAsync();
            _state.Organizations = await _api.GetOrganizationsAsync();
            _state.EnsureSelection();

            Application.Current!.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't load your account: {ex.Message}");
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        Busy.IsRunning = busy;
        Busy.IsVisible = busy;
        SignInButton.IsVisible = !busy;
        if (busy)
        {
            ErrorLabel.IsVisible = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
