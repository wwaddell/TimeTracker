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

        // List every user in the system. Lets global admins edit profile fields for
        // accounts that don't belong to any org (where the per-org Users page can't
        // reach them) — e.g. someone who signed up via Entra without a display name.
        api.MapGet("/users", async (TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }

            var users = await db.Users.AsNoTracking()
                .OrderBy(u => u.DisplayName).ThenBy(u => u.Email)
                .Select(u => new AdminUserDto(
                    u.Id,
                    u.DisplayName,
                    u.Email,
                    u.ExternalId == null,
                    u.IsGlobalAdmin,
                    u.Organizations.Count))
                .ToListAsync();

            return Results.Ok(users);
        });

        // Edit a user as a global admin: display name, email, and the platform-admin flag.
        // Email collisions across the t_user table are rejected (the Email column is the
        // global identity, not org-scoped). A global admin cannot revoke their own admin
        // flag here — that would lock them out — they have to grant another global admin
        // and have that other admin revoke them. Catches the common "I demoted myself"
        // foot-gun.
        api.MapPut("/users/{userId:int}", async (
            int userId, UpdateAdminUserRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.IsGlobalAdminAsync())
            {
                return Results.Forbid();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            var displayName = req.DisplayName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(displayName))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["displayName"] = ["Display name is required."],
                });
            }
            if (displayName.Length > DisplayNameMaxLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["displayName"] = [$"Display name must be {DisplayNameMaxLength} characters or fewer."],
                });
            }

            var email = req.Email?.Trim() ?? string.Empty;
            if (ValidateEmail(email) is { } emailErr) return emailErr;

            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var collision = await db.Users.AnyAsync(u => u.Id != userId && u.Email == email);
                if (collision)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["email"] = ["Another user already has this email address."],
                    });
                }
            }

            // Self-demotion guard. The caller can flip the flag on any OTHER user, but not
            // strip themselves of global-admin. Without this, a single-global-admin setup
            // becomes unrecoverable from the UI (would need direct SQL).
            var callerId = await currentUser.GetUserIdAsync();
            if (callerId == userId && user.IsGlobalAdmin && !req.IsGlobalAdmin)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["isGlobalAdmin"] = ["Have another global admin revoke this — you can't demote yourself."],
                });
            }

            user.DisplayName = displayName;
            user.Email = email;
            user.IsGlobalAdmin = req.IsGlobalAdmin;
            user.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { user.Id });
        });
    }

    // Length caps mirror MemberEndpoints. Centralizing them later would be nice; for now,
    // duplicate the constants alongside their validation here so both endpoint groups
    // remain self-contained.
    private const int DisplayNameMaxLength = 200;
    private const int EmailMaxLength = 320;

    // Same shape as MemberEndpoints.ValidateEmail — duplicated rather than reaching across
    // file boundaries for a small private helper.
    private static IResult? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["A valid email address is required."],
            });
        }
        if (email.Length > EmailMaxLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = [$"Email must be {EmailMaxLength} characters or fewer."],
            });
        }
        return null;
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
