using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TimeTracker.Api.Auth;

/// <summary>
/// DEVELOPMENT-ONLY authentication backdoor. Authenticates every request as a
/// configurable stand-in user so the app can be driven without a real Entra login.
/// Never registered outside Development (guarded in Program.cs).
///
/// Impersonation for testing — override per request with headers:
///   X-Dev-Sub:   subject/external id (default from Auth:DevUser:Sub)
///   X-Dev-Email: email
///   X-Dev-Name:  display name
///   X-Dev-Admin: "true"/"false" — whether the user holds the admin role
/// </summary>
public sealed class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevBypass";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sub = Header("X-Dev-Sub") ?? configuration["Auth:DevUser:Sub"] ?? "dev-local-user";
        var email = Header("X-Dev-Email") ?? configuration["Auth:DevUser:Email"] ?? "dev@local";
        var name = Header("X-Dev-Name") ?? configuration["Auth:DevUser:Name"] ?? "Dev User";

        var adminHeader = Header("X-Dev-Admin");
        var isAdmin = adminHeader is not null
            ? adminHeader.Equals("true", StringComparison.OrdinalIgnoreCase)
            : configuration.GetValue("Auth:DevUser:Admin", true);

        var claims = new List<Claim>
        {
            new("sub", sub),
            new(ClaimTypes.NameIdentifier, sub),
            new("email", email),
            new("name", name),
        };
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "admin"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? Header(string name) =>
        Request.Headers.TryGetValue(name, out var v) ? v.ToString() : null;
}
