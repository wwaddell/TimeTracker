namespace TimeTracker.Api.Calendar;

/// <summary>
/// Microsoft Graph / Entra app-registration settings for the "Connect Outlook" OAuth flow.
/// Bound from the <c>Graph</c> configuration section. Keep the secret in user-secrets / Key Vault,
/// not in appsettings. When <see cref="IsConfigured"/> is false the app falls back to dev sample data.
/// </summary>
public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    /// <summary>Application (client) id of the registered Entra app.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret of the registered app.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Tenant to authenticate against. "common" = multi-tenant (work/school accounts).</summary>
    public string TenantId { get; set; } = "common";

    /// <summary>
    /// OAuth redirect URI registered on the app. Must point at this API's callback,
    /// e.g. https://localhost:7xxx/api/calendar/callback. If blank, it's derived from the request.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Delegated scopes requested: OIDC claims (for the account email), a refresh token
    /// (offline_access), and read-only calendar.
    /// </summary>
    public string Scopes { get; set; } = "openid profile email offline_access User.Read Calendars.Read";

    /// <summary>True once a client id + secret are present (i.e. the real flow can run).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
