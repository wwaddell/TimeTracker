namespace TimeTracker.Mobile;

/// <summary>
/// Static client configuration. These values aren't secrets — the client id + authority
/// ship in any public client app — but they vary per environment, so they're isolated here
/// for a one-line change when pointing at a different backend.
/// </summary>
public static class AppConfig
{
    /// <summary>Base URL of the TimeTracker API (the same host that serves the web app).</summary>
    public const string ApiBaseUrl = "https://time.wrdata.com";

    /// <summary>The Entra External ID app registration (shared with the web SPA).</summary>
    public const string ClientId = "2ee4b766-35b8-4402-ab20-d15d0405c621";

    /// <summary>
    /// CIAM authority. MSAL appends the OIDC discovery path itself — no trailing /v2.0
    /// (mirrors the quirk the web client hit).
    /// </summary>
    public const string Authority = "https://wrdatacom.ciamlogin.com/5b0a8693-70aa-45a4-b5f5-db742314e5c4";

    /// <summary>API scope exposed by the registration's "Expose an API" — grants an access
    /// token whose audience is our API rather than a bare ID token.</summary>
    public static readonly string[] Scopes = [$"api://{ClientId}/access_as_user"];

    /// <summary>
    /// Native redirect URI. Must be registered on the app registration as a "Mobile and
    /// desktop applications" platform redirect (see MOBILE_SETUP.md). The msal-prefixed
    /// scheme is MSAL.NET's convention and is matched by the Android BrowserTabActivity.
    /// </summary>
    public static string RedirectUri => $"msal{ClientId}://auth";
}
