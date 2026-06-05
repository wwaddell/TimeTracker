using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Auth;

/// <summary>Resolves the authenticated application user and their org-scoped rights.</summary>
public interface ICurrentUser
{
    /// <summary>App user id, get-or-created from the token's subject claim. Cached per request.</summary>
    Task<int> GetUserIdAsync();

    /// <summary>Whether the caller holds the dev/global admin role claim (backdoor ⇒ all rights).</summary>
    bool IsAdmin { get; }

    /// <summary>Platform admin: the dev/admin claim, or the user's persisted IsGlobalAdmin flag.</summary>
    Task<bool> IsGlobalAdminAsync();

    /// <summary>Ids of the roles the current user holds in the given org.</summary>
    Task<IReadOnlyList<int>> GetRoleIdsAsync(int orgId);

    /// <summary>Whether the current user has a given right in the org (admin claim ⇒ true).</summary>
    Task<bool> HasRightAsync(int orgId, OrgRight right);
}

public sealed class CurrentUser(IHttpContextAccessor accessor, TimeTrackerDbContext db) : ICurrentUser
{
    private int? _cachedId;

    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No authenticated context.");

    public bool IsAdmin => Principal.IsInRole("admin");

    public async Task<bool> IsGlobalAdminAsync()
    {
        // Dev backdoor / admin claim ⇒ platform admin; otherwise the persisted user flag.
        if (IsAdmin)
        {
            return true;
        }

        var id = await GetUserIdAsync();
        return await db.Users.Where(u => u.Id == id).Select(u => u.IsGlobalAdmin).FirstOrDefaultAsync();
    }

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
        var email = p.FindFirstValue("email") ?? p.FindFirstValue(ClaimTypes.Email);
        var name = p.FindFirstValue("name") ?? p.FindFirstValue(ClaimTypes.Name);

        // 1) Returning user — match by external id.
        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);

        // 2) Invited user — a pending record (no external id) with this email; link it now.
        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == null && u.Email == email);
            if (user is not null)
            {
                user.ExternalId = externalId;
                user.ModifiedUtc = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(name))
                {
                    user.DisplayName = name;
                }
                await db.SaveChangesAsync();
            }
        }

        // 3) Brand-new user — provision.
        if (user is null)
        {
            user = new User
            {
                ExternalId = externalId,
                Email = email ?? string.Empty,
                DisplayName = name ?? email ?? externalId,
                CreatedUtc = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        _cachedId = user.Id;
        return user.Id;
    }

    public async Task<IReadOnlyList<int>> GetRoleIdsAsync(int orgId)
    {
        var userId = await GetUserIdAsync();
        return await db.UserOrganizationRoles
            .Where(uor => uor.UserOrganization.UserId == userId && uor.UserOrganization.OrganizationId == orgId)
            .Select(uor => uor.OrganizationRoleId)
            .ToListAsync();
    }

    public async Task<bool> HasRightAsync(int orgId, OrgRight right)
    {
        // Dev backdoor / global admin grants every right so the app stays fully testable.
        if (IsAdmin)
        {
            return true;
        }

        var roleIds = await GetRoleIdsAsync(orgId);
        if (roleIds.Count == 0)
        {
            return false;
        }

        return await db.OrganizationRoleRights
            .AnyAsync(rr => roleIds.Contains(rr.OrganizationRoleId) && rr.Right == right);
    }
}
