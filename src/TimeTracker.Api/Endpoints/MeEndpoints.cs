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
            return Results.Ok(new MeDto(user.Id, user.DisplayName, user.Email, isGlobalAdmin));
        });
    }
}
