using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Roles;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("").RequireAuthorization();

        // Roles with their rights + member counts.
        grp.MapGet("/api/organizations/{orgId:int}/roles/admin",
            async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageRoles))
            {
                return Results.Forbid();
            }

            var roles = await db.OrganizationRoles
                .Where(r => r.OrganizationId == orgId)
                .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                .Select(r => new RoleAdminDto(
                    r.Id, r.Name, r.SortOrder, r.Members.Count,
                    r.Rights.Select(x => x.Right).OrderBy(x => x).ToList()))
                .ToListAsync();

            return Results.Ok(roles);
        });

        // Members holding a given role.
        grp.MapGet("/api/organizations/{orgId:int}/roles/{roleId:int}/members",
            async (int orgId, int roleId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageRoles))
            {
                return Results.Forbid();
            }

            var members = await db.UserOrganizationRoles
                .Where(uor => uor.OrganizationRoleId == roleId && uor.UserOrganization.OrganizationId == orgId)
                .OrderBy(uor => uor.UserOrganization.User.DisplayName)
                .Select(uor => new RoleMemberDto(
                    uor.UserOrganization.UserId,
                    uor.UserOrganization.User.Email,
                    uor.UserOrganization.User.DisplayName))
                .ToListAsync();

            return Results.Ok(members);
        });

        grp.MapPost("/api/organizations/{orgId:int}/roles",
            async (int orgId, SaveRoleRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageRoles))
            {
                return Results.Forbid();
            }

            var validation = await ValidateAsync(db, orgId, req, roleId: null);
            if (validation is not null)
            {
                return validation;
            }

            var role = new OrganizationRole
            {
                OrganizationId = orgId,
                Name = req.Name.Trim(),
                SortOrder = req.SortOrder,
                CreatedUtc = DateTime.UtcNow,
            };
            foreach (var right in req.Rights.Distinct())
            {
                role.Rights.Add(new OrganizationRoleRight { Right = right });
            }

            db.OrganizationRoles.Add(role);
            await db.SaveChangesAsync();
            return Results.Created($"/api/organizations/{orgId}/roles/{role.Id}", new { role.Id });
        });

        grp.MapPut("/api/organizations/{orgId:int}/roles/{roleId:int}",
            async (int orgId, int roleId, SaveRoleRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageRoles))
            {
                return Results.Forbid();
            }

            var role = await db.OrganizationRoles
                .Include(r => r.Rights)
                .FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == orgId);
            if (role is null)
            {
                return Results.NotFound();
            }

            var validation = await ValidateAsync(db, orgId, req, roleId);
            if (validation is not null)
            {
                return validation;
            }

            role.Name = req.Name.Trim();
            role.SortOrder = req.SortOrder;
            role.ModifiedUtc = DateTime.UtcNow;

            db.OrganizationRoleRights.RemoveRange(role.Rights);
            role.Rights.Clear();
            foreach (var right in req.Rights.Distinct())
            {
                role.Rights.Add(new OrganizationRoleRight { Right = right });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { role.Id });
        });

        grp.MapDelete("/api/organizations/{orgId:int}/roles/{roleId:int}",
            async (int orgId, int roleId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageRoles))
            {
                return Results.Forbid();
            }

            var role = await db.OrganizationRoles
                .Include(r => r.Rights)
                .FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == orgId);
            if (role is null)
            {
                return Results.NotFound();
            }

            if (await db.UserOrganizationRoles.AnyAsync(uor => uor.OrganizationRoleId == roleId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["members"] = ["This role is still assigned to members. Remove it from them first."],
                });
            }

            if (await db.TimeEntryFields.AnyAsync(f => f.OrganizationRoleId == roleId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fields"] = ["This role is used to scope a configurable field. Update those fields first."],
                });
            }

            db.OrganizationRoleRights.RemoveRange(role.Rights);
            db.OrganizationRoles.Remove(role);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult?> ValidateAsync(
        TimeTrackerDbContext db, int orgId, SaveRoleRequest req, int? roleId)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Role name is required."],
            });
        }

        var name = req.Name.Trim();
        if (await db.OrganizationRoles.AnyAsync(r => r.OrganizationId == orgId && r.Name == name && r.Id != roleId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["A role with this name already exists in this organization."],
            });
        }

        return null;
    }
}
