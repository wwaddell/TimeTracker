using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly DataService _data;
    private readonly AppState _state;
    private bool _triedSilent;

    public LoginPage(AuthService auth, DataService data, AppState state)
    {
        InitializeComponent();
        _auth = auth;
        _data = data;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_triedSilent)
        {
            return;
        }
        _triedSilent = true;

        // Returning user with a cached token skips the button.
        if (await _auth.IsSignedInAsync())
        {
            await CompleteSignInAsync();
        }
        else if (!DataService.IsOnline)
        {
            // Offline + previously set up: let them straight into the cached app without a
            // fresh token (the API calls will just fail/queue until they're back online).
            await _state.LoadOrgsAsync(_data);
            if (_state.Organizations.Count > 0)
            {
                EnterApp();
            }
            else
            {
                ShowError("You're offline and the app hasn't been set up yet. Connect to the internet and sign in once.");
            }
        }
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        if (!DataService.IsOnline)
        {
            ShowError("You're offline. Connect to the internet to sign in the first time.");
            return;
        }
        SetBusy(true);
        try
        {
            var token = await _auth.GetAccessTokenAsync(allowInteractive: true);
            if (string.IsNullOrEmpty(token))
            {
                SetBusy(false); // user cancelled
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

    // Pull orgs + the selected org's data into the local cache, then enter the app.
    private async Task CompleteSignInAsync()
    {
        SetBusy(true);
        try
        {
            if (DataService.IsOnline)
            {
                await _data.SyncAsync(0);                 // orgs only
                await _state.LoadOrgsAsync(_data);
                if (_state.SelectedOrgId != 0)
                {
                    await _data.SyncAsync(_state.SelectedOrgId); // that org's projects/tasks/entries
                }
            }
            else
            {
                await _state.LoadOrgsAsync(_data);
            }
            EnterApp();
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't load your account: {ex.Message}");
            SetBusy(false);
        }
    }

    private void EnterApp() => Application.Current!.Windows[0].Page = new AppShell();

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
        SetBusy(false);
    }
}
