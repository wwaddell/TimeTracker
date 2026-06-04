using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Members;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("").RequireAuthorization();

        // List members of an org with the roles each holds.
        grp.MapGet("/api/organizations/{orgId:int}/members",
            async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            var members = await db.UserOrganizations
                .Where(m => m.OrganizationId == orgId)
                .OrderBy(m => m.User.DisplayName).ThenBy(m => m.User.Email)
                .Select(m => new MemberDto(
                    m.UserId,
                    m.User.Email,
                    m.User.DisplayName,
                    m.User.ExternalId == null,
                    m.IsDefault,
                    m.Roles.OrderBy(r => r.OrganizationRole.Name)
                        .Select(r => new RoleRefDto(r.OrganizationRoleId, r.OrganizationRole.Name)).ToList()))
                .ToListAsync();

            return Results.Ok(members);
        });

        // Invite/add a member by email with an initial role set.
        grp.MapPost("/api/organizations/{orgId:int}/members",
            async (int orgId, InviteMemberRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            var email = req.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["A valid email address is required."],
                });
            }

            var roleError = await ValidateRolesAsync(db, orgId, req.RoleIds);
            if (roleError is not null)
            {
                return roleError;
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? email : req.DisplayName.Trim(),
                    ExternalId = null, // pending until first login
                    CreatedUtc = DateTime.UtcNow,
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var membership = await db.UserOrganizations
                .Include(m => m.Roles)
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == orgId);
            if (membership is null)
            {
                membership = new UserOrganization { UserId = user.Id, OrganizationId = orgId, CreatedUtc = DateTime.UtcNow };
                db.UserOrganizations.Add(membership);
                await db.SaveChangesAsync();
            }

            var existing = membership.Roles.Select(r => r.OrganizationRoleId).ToHashSet();
            foreach (var roleId in req.RoleIds.Distinct().Where(id => !existing.Contains(id)))
            {
                db.UserOrganizationRoles.Add(new UserOrganizationRole
                {
                    UserOrganizationId = membership.Id,
                    OrganizationRoleId = roleId,
                });
            }
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizations/{orgId}/members/{user.Id}", new { user.Id });
        });

        // Replace the set of roles a member holds.
        grp.MapPut("/api/organizations/{orgId:int}/members/{userId:int}/roles",
            async (int orgId, int userId, SetMemberRolesRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            var membership = await db.UserOrganizations
                .Include(m => m.Roles)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.NotFound();
            }

            var roleError = await ValidateRolesAsync(db, orgId, req.RoleIds);
            if (roleError is not null)
            {
                return roleError;
            }

            db.UserOrganizationRoles.RemoveRange(membership.Roles);
            foreach (var roleId in req.RoleIds.Distinct())
            {
                db.UserOrganizationRoles.Add(new UserOrganizationRole
                {
                    UserOrganizationId = membership.Id,
                    OrganizationRoleId = roleId,
                });
            }
            await db.SaveChangesAsync();

            return Results.Ok(new { membership.Id });
        });

        // Remove a member from the org (keeps the user record; they may be in other orgs).
        grp.MapDelete("/api/organizations/{orgId:int}/members/{userId:int}",
            async (int orgId, int userId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            var membership = await db.UserOrganizations
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.NotFound();
            }

            db.UserOrganizations.Remove(membership); // role links cascade
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult?> ValidateRolesAsync(TimeTrackerDbContext db, int orgId, List<int> roleIds)
    {
        if (roleIds.Count == 0)
        {
            return null;
        }

        var validIds = await db.OrganizationRoles
            .Where(r => r.OrganizationId == orgId)
            .Select(r => r.Id)
            .ToListAsync();

        return roleIds.Distinct().All(validIds.Contains)
            ? null
            : Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["One or more selected roles do not belong to this organization."],
            });
    }
}
