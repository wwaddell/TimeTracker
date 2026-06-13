using System.Net.Http.Headers;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// Attaches the MSAL access token as a Bearer header to every API request. Acquired
/// silently per-call (MSAL caches + refreshes internally); does NOT trigger an
/// interactive prompt mid-request — if the token can't be obtained silently the call
/// goes out unauthenticated and the API returns 401, which the UI handles by routing
/// back to sign-in.
/// </summary>
public class AuthHttpMessageHandler(AuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await auth.GetAccessTokenAsync(allowInteractive: false);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
