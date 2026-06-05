using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Admin;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

/// <summary>
/// Platform-level organization management for global admins: create/list/update orgs and
/// assign their administrators. An "org admin" is a member holding a role that grants
/// <see cref="OrgRight.ManageOrganization"/>; assigning one ensures an Administrator role
/// (all rights) exists and links the user to it.
/// </summary>
public static class GlobalAdminEndpoints
{
    public static void MapGlobalAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/admin").RequireAuthorization();

        // List every organization in the system.
        api.MapGet("/organizations", async (TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }

            var orgs = await db.Organizations
                .OrderBy(o => o.Name)
                .Select(o => new AdminOrgDto(o.Id, o.Name, o.Description, o.IsActive, o.Members.Count))
                .ToListAsync();

            return Results.Ok(orgs);
        });

        // Create a new organization, seed an Administrator role (all rights), and make the
        // creating global admin a member-admin so they can manage it.
        api.MapPost("/organizations", async (
            CreateOrganizationRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["An organization name is required."],
                });
            }

            var org = new Organization
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                CreatedUtc = DateTime.UtcNow,
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();

            var role = await EnsureAdministratorRoleAsync(db, org.Id);
            var creatorId = await currentUser.GetUserIdAsync();
            await EnsureMembershipWithRoleAsync(db, creatorId, org.Id, role.Id);

            return Results.Created($"/api/admin/organizations/{org.Id}", new { org.Id });
        });

        // Update org details.
        api.MapPut("/organizations/{orgId:int}", async (
            int orgId, UpdateOrganizationRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["An organization name is required."],
                });
            }

            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org is null)
            {
                return Results.NotFound();
            }

            org.Name = request.Name.Trim();
            org.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            org.IsActive = request.IsActive;
            org.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { org.Id });
        });

        // List the org's administrators (members holding a ManageOrganization-granting role).
        api.MapGet("/organizations/{orgId:int}/admins", async (
            int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }

            var admins = await db.UserOrganizationRoles
                .Where(uor => uor.UserOrganization.OrganizationId == orgId
                    && uor.OrganizationRole.Rights.Any(r => r.Right == OrgRight.ManageOrganization))
                .Select(uor => uor.UserOrganization.User)
                .Distinct()
                .OrderBy(u => u.DisplayName)
                .Select(u => new OrgAdminDto(u.Id, u.DisplayName, u.Email, u.ExternalId == null))
                .ToListAsync();

            return Results.Ok(admins);
        });

        // Assign an admin to the org by email (created pending if the user doesn't exist yet).
        api.MapPost("/organizations/{orgId:int}/admins", async (
            int orgId, AssignOrgAdminRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["A valid email is required."],
                });
            }
            if (!await db.Organizations.AnyAsync(o => o.Id == orgId))
            {
                return Results.NotFound();
            }

            var email = request.Email.Trim();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName!.Trim(),
                    ExternalId = null, // pending until first sign-in
                    CreatedUtc = DateTime.UtcNow,
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var role = await EnsureAdministratorRoleAsync(db, orgId);
            await EnsureMembershipWithRoleAsync(db, user.Id, orgId, role.Id);

            return Results.Ok(new { user.Id });
        });

        // Revoke admin: remove the user's assignment to any ManageOrganization-granting role.
        api.MapDelete("/organizations/{orgId:int}/admins/{userId:int}", async (
            int orgId, int userId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }

            var adminLinks = await db.UserOrganizationRoles
                .Where(uor => uor.UserOrganization.OrganizationId == orgId
                    && uor.UserOrganization.UserId == userId
                    && uor.OrganizationRole.Rights.Any(r => r.Right == OrgRight.ManageOrganization))
                .ToListAsync();

            if (adminLinks.Count == 0)
            {
                return Results.NotFound();
            }

            db.UserOrganizationRoles.RemoveRange(adminLinks);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // Find (or create) an "Administrator" role granting all rights for the org.
    private static async Task<OrganizationRole> EnsureAdministratorRoleAsync(TimeTrackerDbContext db, int orgId)
    {
        var role = await db.OrganizationRoles
            .Include(r => r.Rights)
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Name == "Administrator");
        if (role is not null)
        {
            return role;
        }

        role = new OrganizationRole
        {
            OrganizationId = orgId,
            Name = "Administrator",
            SortOrder = 1,
            CreatedUtc = DateTime.UtcNow,
        };
        foreach (var right in Enum.GetValues<OrgRight>())
        {
            role.Rights.Add(new OrganizationRoleRight { Right = right });
        }
        db.OrganizationRoles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    // Ensure the user is a member of the org and holds the given role.
    private static async Task EnsureMembershipWithRoleAsync(TimeTrackerDbContext db, int userId, int orgId, int roleId)
    {
        var membership = await db.UserOrganizations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
        if (membership is null)
        {
            membership = new UserOrganization { UserId = userId, OrganizationId = orgId, CreatedUtc = DateTime.UtcNow };
            db.UserOrganizations.Add(membership);
            await db.SaveChangesAsync();
        }

        var hasRole = await db.UserOrganizationRoles
            .AnyAsync(x => x.UserOrganizationId == membership.Id && x.OrganizationRoleId == roleId);
        if (!hasRole)
        {
            db.UserOrganizationRoles.Add(new UserOrganizationRole
            {
                UserOrganizationId = membership.Id,
                OrganizationRoleId = roleId,
            });
            await db.SaveChangesAsync();
        }
    }
}
