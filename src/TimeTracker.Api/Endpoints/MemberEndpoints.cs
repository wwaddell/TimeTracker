using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Members;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class MemberEndpoints
{
    // Length caps match UserConfiguration. Validated at the endpoint so the caller gets a 400
    // ProblemDetails rather than an EF SqlException at SaveChanges.
    private const int DisplayNameMaxLength = 200;
    private const int EmailMaxLength = 320;

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
            async (int orgId, InviteMemberRequest req, TimeTrackerDbContext db,
                   ICurrentUser currentUser, ILoggerFactory loggerFactory) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            var email = req.Email?.Trim() ?? string.Empty;
            if (ValidateEmail(email) is { } emailErr) return emailErr;

            var displayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim();
            if (displayName is { Length: > DisplayNameMaxLength })
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["displayName"] = [$"Display name must be {DisplayNameMaxLength} characters or fewer."],
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
                    DisplayName = displayName ?? email,
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

            // Side effect: invite email. Wire-ready, not yet wired. Drops a structured log
            // entry so it shows up in the dev console; replace the inner call once an
            // IEmailSender service is registered.
            if (req.SendInvite)
            {
                SendInviteEmailStub(loggerFactory.CreateLogger("InviteEmail"), user.Email, user.DisplayName, orgId);
            }

            return Results.Created($"/api/organizations/{orgId}/members/{user.Id}", new { user.Id });
        });

        // Edit a member's profile fields (DisplayName / Email). Roles are managed via the
        // separate /roles endpoint below.
        grp.MapPut("/api/organizations/{orgId:int}/members/{userId:int}",
            async (int orgId, int userId, UpdateMemberProfileRequest req, TimeTrackerDbContext db,
                   ICurrentUser currentUser, ILoggerFactory loggerFactory) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageUsers))
            {
                return Results.Forbid();
            }

            // Must be a member of THIS org — admins in org A can't edit a user just because
            // that user belongs to org B as well. Include User so we can compare the existing
            // email below without a second query.
            var membership = await db.UserOrganizations
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.NotFound();
            }
            var user = membership.User;

            var email = req.Email?.Trim() ?? string.Empty;
            if (ValidateEmail(email) is { } emailErr) return emailErr;

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

            // The User row is global — changing email here changes it for every org the user
            // belongs to. Reject the change if another user already has the new email.
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

            user.DisplayName = displayName;
            user.Email = email;
            user.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            if (req.SendInvite)
            {
                SendInviteEmailStub(loggerFactory.CreateLogger("InviteEmail"), user.Email, user.DisplayName, orgId);
            }

            return Results.Ok(new { user.Id });
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

    // Bare-minimum email validation — a more permissive check than throwing the whole
    // RFC 5322 grammar at it. EF + UserConfiguration enforce the 320-char cap.
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

    // Stub for the invite-email side effect. No SMTP integration yet — when one is added
    // (e.g. an IEmailSender DI service), replace the body to send a real templated message.
    // The structured log keeps the action visible during development.
    private static void SendInviteEmailStub(ILogger logger, string email, string displayName, int orgId)
    {
        logger.LogInformation(
            "[InviteEmail-STUB] Would send invitation to {Email} ({DisplayName}) for org {OrgId}. " +
            "Wire an IEmailSender to make this real.",
            email, displayName, orgId);
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
