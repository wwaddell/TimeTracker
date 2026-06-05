using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TimeTracker.Api.Calendar;

/// <summary>Tokens returned by the Entra token endpoint.</summary>
public sealed record OAuthTokens(string AccessToken, string? RefreshToken, int ExpiresIn, string? Scope, string? IdToken)
{
    /// <summary>Connected account's email/UPN, parsed from the id_token if present.</summary>
    public string? AccountEmail => OutlookOAuthClient.ReadIdTokenClaim(IdToken, "preferred_username")
        ?? OutlookOAuthClient.ReadIdTokenClaim(IdToken, "email");

    /// <summary>Account home tenant id, parsed from the id_token if present.</summary>
    public string? TenantId => OutlookOAuthClient.ReadIdTokenClaim(IdToken, "tid");
}

/// <summary>
/// Minimal OAuth2 client for the "Connect Outlook" flow against Microsoft Entra (authorization
/// code + refresh, confidential client). Kept dependency-free (raw HttpClient) for transparency;
/// could be swapped for MSAL later. Used server-side; the client secret never leaves the API.
/// </summary>
public sealed class OutlookOAuthClient(HttpClient http, IOptions<GraphOptions> options)
{
    private GraphOptions O => options.Value;
    private string Authority => $"https://login.microsoftonline.com/{O.TenantId}";

    /// <summary>User sign-in + consent URL (authorization code flow).</summary>
    public string BuildAuthorizeUrl(string redirectUri, string state) =>
        $"{Authority}/oauth2/v2.0/authorize"
        + $"?client_id={Enc(O.ClientId)}"
        + "&response_type=code"
        + $"&redirect_uri={Enc(redirectUri)}"
        + "&response_mode=query"
        + $"&scope={Enc(O.Scopes)}"
        + $"&state={Enc(state)}";

    /// <summary>Admin consent URL — an org admin grants the app tenant-wide in one click.</summary>
    public string BuildAdminConsentUrl(string redirectUri, string state) =>
        $"{Authority}/v2.0/adminconsent"
        + $"?client_id={Enc(O.ClientId)}"
        + $"&scope={Enc("https://graph.microsoft.com/.default")}"
        + $"&redirect_uri={Enc(redirectUri)}"
        + $"&state={Enc(state)}";

    public Task<OAuthTokens> RedeemCodeAsync(string code, string redirectUri, CancellationToken ct = default) =>
        PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = O.ClientId,
            ["client_secret"] = O.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["scope"] = O.Scopes,
        }, ct);

    public Task<OAuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = O.ClientId,
            ["client_secret"] = O.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = O.Scopes,
        }, ct);

    private async Task<OAuthTokens> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var response = await http.PostAsync($"{Authority}/oauth2/v2.0/token", new FormUrlEncodedContent(form), ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (!response.IsSuccessStatusCode)
        {
            var desc = Str(root, "error_description") ?? Str(root, "error") ?? "Token request failed.";
            throw new CalendarSourceException($"Outlook sign-in failed: {desc}");
        }

        return new OAuthTokens(
            AccessToken: Str(root, "access_token") ?? throw new CalendarSourceException("No access token returned."),
            RefreshToken: Str(root, "refresh_token"),
            ExpiresIn: root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var s) ? s : 3600,
            Scope: Str(root, "scope"),
            IdToken: Str(root, "id_token"));
    }

    /// <summary>Reads a claim from an unvalidated id_token payload (display info only).</summary>
    public static string? ReadIdTokenClaim(string? idToken, string claim)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return null;
        }

        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Base64Url(parts[1]));
            using var doc = JsonDocument.Parse(json);
            return Str(doc.RootElement, claim);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private static string? Str(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Enc(string s) => Uri.EscapeDataString(s);
}
