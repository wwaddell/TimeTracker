using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Auth;

/// <summary>Resolves the authenticated application user, provisioning one on first sign-in.</summary>
public interface ICurrentUser
{
    /// <summary>App user id, get-or-created from the token's subject claim. Cached per request.</summary>
    Task<int> GetUserIdAsync();

    /// <summary>Whether the caller holds the admin role.</summary>
    bool IsAdmin { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor, TimeTrackerDbContext db) : ICurrentUser
{
    private int? _cachedId;

    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No authenticated context.");

    public bool IsAdmin => Principal.IsInRole("admin");

    public async Task<int> GetUserIdAsync()
    {
        if (_cachedId is { } cached)
        {
            return cached;
        }

        var p = Principal;
        var externalId = p.FindFirstValue("sub")
            ?? p.FindFirstValue("oid")
            ?? p.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated principal has no subject claim.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
        if (user is null)
        {
            user = new User
            {
                ExternalId = externalId,
                Email = p.FindFirstValue("email") ?? p.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                DisplayName = p.FindFirstValue("name") ?? p.FindFirstValue(ClaimTypes.Name) ?? externalId,
                CreatedUtc = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        _cachedId = user.Id;
        return user.Id;
    }
}
