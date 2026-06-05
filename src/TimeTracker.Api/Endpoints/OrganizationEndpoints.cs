using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Organizations;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("").RequireAuthorization();

        // Organizations the current user may manage (holds ManageOrganization; dev/global admin
        // sees all their member orgs). Powers the list-based Organization admin page.
        grp.MapGet("/api/organizations/manageable",
            async (TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();

            var query = currentUser.IsAdmin
                ? db.Organizations.Where(o => o.Members.Any(m => m.UserId == userId))
                : db.Organizations.Where(o => o.Members.Any(m => m.UserId == userId
                    && m.Roles.Any(r => r.OrganizationRole.Rights.Any(rr => rr.Right == OrgRight.ManageOrganization))));

            var orgs = await query
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationDetailsDto(o.Id, o.Name, o.Description, o.IsActive))
                .ToListAsync();

            return Results.Ok(orgs);
        });

        grp.MapGet("/api/organizations/{orgId:int}/details",
            async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageOrganization))
            {
                return Results.Forbid();
            }

            var org = await db.Organizations
                .Where(o => o.Id == orgId)
                .Select(o => new OrganizationDetailsDto(o.Id, o.Name, o.Description, o.IsActive))
                .FirstOrDefaultAsync();

            return org is null ? Results.NotFound() : Results.Ok(org);
        });

        grp.MapPut("/api/organizations/{orgId:int}/details",
            async (int orgId, SaveOrganizationRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageOrganization))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Organization name is required."],
                });
            }

            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org is null)
            {
                return Results.NotFound();
            }

            org.Name = req.Name.Trim();
            org.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            org.IsActive = req.IsActive;
            org.ModifiedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { org.Id });
        });
    }
}
