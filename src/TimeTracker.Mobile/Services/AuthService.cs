using Microsoft.Identity.Client;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// Wraps MSAL's PublicClientApplication for native interactive sign-in against Entra
/// External ID. Tries a silent token first (cached account), falling back to an
/// interactive browser flow. The acquired access token is attached to API calls by
/// <see cref="AuthHttpMessageHandler"/>.
/// </summary>
public class AuthService
{
    private readonly IPublicClientApplication _pca;

    public AuthService()
    {
        var builder = PublicClientApplicationBuilder
            .Create(AppConfig.ClientId)
            .WithAuthority(AppConfig.Authority)
            .WithRedirectUri(AppConfig.RedirectUri);

#if ANDROID
        // Required so MSAL can launch + return from the system browser / custom tab.
        builder = builder.WithParentActivityOrWindow(() => Platform.CurrentActivity);
#endif

        _pca = builder.Build();
    }

    /// <summary>The most recently signed-in account, or null if none is cached.</summary>
    public async Task<IAccount?> GetCachedAccountAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        return accounts.FirstOrDefault();
    }

    /// <summary>True if a token can be obtained silently (already signed in).</summary>
    public async Task<bool> IsSignedInAsync()
    {
        var account = await GetCachedAccountAsync();
        if (account is null)
        {
            return false;
        }
        try
        {
            await _pca.AcquireTokenSilent(AppConfig.Scopes, account).ExecuteAsync();
            return true;
        }
        catch (MsalUiRequiredException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns a valid access token: silent if possible, otherwise interactive. Throws
    /// <see cref="MsalClientException"/>/<see cref="MsalServiceException"/> on real failures;
    /// returns null only if the user cancels the interactive prompt.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(bool allowInteractive = true)
    {
        var account = await GetCachedAccountAsync();
        try
        {
            if (account is not null)
            {
                var silent = await _pca.AcquireTokenSilent(AppConfig.Scopes, account).ExecuteAsync();
                return silent.AccessToken;
            }
        }
        catch (MsalUiRequiredException)
        {
            // fall through to interactive
        }

        if (!allowInteractive)
        {
            return null;
        }

        try
        {
            var result = await _pca.AcquireTokenInteractive(AppConfig.Scopes).ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        foreach (var account in await _pca.GetAccountsAsync())
        {
            await _pca.RemoveAsync(account);
        }
    }
}
