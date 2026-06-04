using Microsoft.EntityFrameworkCore;
using TimeTracker.Contracts.Rights;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class RightsEndpoints
{
    public static void MapRightsEndpoints(this IEndpointRouteBuilder app)
    {
        // The fixed catalog of grantable rights (for the role editor). Any authenticated user.
        app.MapGet("/api/rights", async (TimeTrackerDbContext db) =>
        {
            var rights = await db.Rights
                .OrderBy(r => r.Id)
                .Select(r => new RightDto(r.Id, r.Code, r.Name, r.Description))
                .ToListAsync();
            return Results.Ok(rights);
        }).RequireAuthorization();
    }
}
