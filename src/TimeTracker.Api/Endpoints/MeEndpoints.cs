using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Me;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

/// <summary>Endpoints describing the current authenticated user (for the Profile page).</summary>
public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("").RequireAuthorization();

        api.MapGet("/api/me", async (TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            // IsGlobalAdmin reflects both the dev/admin claim and the persisted flag.
            var isGlobalAdmin = await currentUser.IsGlobalAdminAsync();
            var defaultOrgId = await db.UserOrganizations
                .Where(m => m.UserId == userId && m.IsDefault)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync();
            return Results.Ok(new MeDto(user.Id, user.DisplayName, user.Email,
                isGlobalAdmin, user.HideOrgSwitcher, user.DarkMode, user.CompactMode,
                user.CaptureLocation, (int)user.WeekStartsOn, defaultOrgId));
        });

        // Update the user's personal preferences (default org + show/hide org switcher).
        api.MapPut("/api/me/preferences",
            async (SaveMePreferencesRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();

            // Validate the chosen default org is one the user belongs to (if provided).
            if (request.DefaultOrganizationId is { } orgId)
            {
                var isMember = await db.UserOrganizations
                    .AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
                if (!isMember)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["defaultOrganizationId"] = ["You aren't a member of the selected organization."],
                    });
                }

                // Flip IsDefault across the user's memberships so exactly one is default.
                var memberships = await db.UserOrganizations.Where(m => m.UserId == userId).ToListAsync();
                foreach (var m in memberships)
                {
                    var shouldBeDefault = m.OrganizationId == orgId;
                    if (m.IsDefault != shouldBeDefault)
                    {
                        m.IsDefault = shouldBeDefault;
                        m.ModifiedUtc = DateTime.UtcNow;
                    }
                }
            }

            // Partial update: only apply fields the caller sent (non-null).
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            var changed = false;
            if (request.HideOrgSwitcher is { } h && user.HideOrgSwitcher != h) { user.HideOrgSwitcher = h; changed = true; }
            if (request.DarkMode is { } d && user.DarkMode != d) { user.DarkMode = d; changed = true; }
            if (request.CompactMode is { } c && user.CompactMode != c) { user.CompactMode = c; changed = true; }
            if (request.CaptureLocation is { } cl && user.CaptureLocation != cl) { user.CaptureLocation = cl; changed = true; }
            if (request.WeekStartsOn is { } w)
            {
                if (w is < 0 or > 6)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["weekStartsOn"] = ["Week start must be 0 (Sunday) through 6 (Saturday)."],
                    });
                }
                var dow = (DayOfWeek)w;
                if (user.WeekStartsOn != dow) { user.WeekStartsOn = dow; changed = true; }
            }
            if (changed)
            {
                user.ModifiedUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
