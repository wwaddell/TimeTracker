using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Projects;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("").RequireAuthorization();

        // ---- Picker (any org member) ----

        // Projects the current user can pick: active, and either unrestricted OR they're a member.
        api.MapGet("/api/organizations/{orgId:int}/projects/visible", async (
            int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var projects = await db.Projects
                .Where(p => p.OrganizationId == orgId && p.IsActive
                    && (!p.IsRestricted || p.Members.Any(m => m.UserId == userId)))
                .OrderBy(p => p.Name)
                .Select(p => new ProjectPickerDto(p.Id, p.Name, p.ReferenceCode))
                .ToListAsync();

            return Results.Ok(projects);
        });

        // ---- Admin (ManageProjects) ----

        api.MapGet("/api/organizations/{orgId:int}/projects", async (
            int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }

            var projects = await db.Projects
                .Where(p => p.OrganizationId == orgId)
                .OrderBy(p => p.Name)
                .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.IsActive, p.IsRestricted,
                    p.ReferenceCode, p.ExternalUrl, p.Members.Count))
                .ToListAsync();

            return Results.Ok(projects);
        });

        api.MapPost("/api/organizations/{orgId:int}/projects", async (
            int orgId, SaveProjectRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }
            if (Validate(req) is { } problem)
            {
                return problem;
            }

            // Name unique per org (filtered to non-deleted by the index).
            var name = req.Name.Trim();
            if (await db.Projects.AnyAsync(p => p.OrganizationId == orgId && p.Name == name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["A project with this name already exists."],
                });
            }

            var refCode = string.IsNullOrWhiteSpace(req.ReferenceCode) ? null : req.ReferenceCode.Trim();
            if (refCode is not null
                && await db.Projects.AnyAsync(p => p.OrganizationId == orgId && p.ReferenceCode == refCode))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["referenceCode"] = ["Another project already uses this reference code."],
                });
            }

            var project = new Project
            {
                OrganizationId = orgId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                IsActive = req.IsActive,
                IsRestricted = req.IsRestricted,
                ReferenceCode = refCode,
                ExternalUrl = string.IsNullOrWhiteSpace(req.ExternalUrl) ? null : req.ExternalUrl.Trim(),
                CreatedUtc = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizations/{orgId}/projects/{project.Id}", new { project.Id });
        });

        api.MapPut("/api/organizations/{orgId:int}/projects/{id:int}", async (
            int orgId, int id, SaveProjectRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }
            if (Validate(req) is { } problem)
            {
                return problem;
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var name = req.Name.Trim();
            if (project.Name != name && await db.Projects
                    .AnyAsync(p => p.OrganizationId == orgId && p.Name == name && p.Id != id))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["A project with this name already exists."],
                });
            }

            var refCode = string.IsNullOrWhiteSpace(req.ReferenceCode) ? null : req.ReferenceCode.Trim();
            if (refCode is not null && project.ReferenceCode != refCode && await db.Projects
                    .AnyAsync(p => p.OrganizationId == orgId && p.ReferenceCode == refCode && p.Id != id))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["referenceCode"] = ["Another project already uses this reference code."],
                });
            }

            project.Name = name;
            project.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            project.IsActive = req.IsActive;
            project.IsRestricted = req.IsRestricted;
            project.ReferenceCode = refCode;
            project.ExternalUrl = string.IsNullOrWhiteSpace(req.ExternalUrl) ? null : req.ExternalUrl.Trim();
            project.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { project.Id });
        });

        // Soft-delete: project disappears from pickers + admin list; linked entries/tasks keep
        // their FK (project name will read as null via the global filter — see below).
        api.MapDelete("/api/organizations/{orgId:int}/projects/{id:int}", async (
            int orgId, int id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (project is null)
            {
                return Results.NotFound();
            }

            project.DeletedUtc = DateTime.UtcNow;
            project.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- Members of a (restricted) project ----

        api.MapGet("/api/organizations/{orgId:int}/projects/{id:int}/members", async (
            int orgId, int id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }

            var projectExists = await db.Projects.AnyAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (!projectExists)
            {
                return Results.NotFound();
            }

            var members = await db.ProjectMembers
                .Where(m => m.ProjectId == id)
                .OrderBy(m => m.User.DisplayName)
                .Select(m => new ProjectMemberDto(m.UserId, m.User.DisplayName, m.User.Email, m.User.ExternalId == null))
                .ToListAsync();

            return Results.Ok(members);
        });

        // Add a member by email — creates a pending user if the email is new (same pattern as RBAC invite).
        api.MapPost("/api/organizations/{orgId:int}/projects/{id:int}/members", async (
            int orgId, int id, AddProjectMemberRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["A valid email is required."],
                });
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var email = req.Email.Trim();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? email : req.DisplayName!.Trim(),
                    ExternalId = null, // pending until first sign-in
                    CreatedUtc = DateTime.UtcNow,
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var alreadyMember = await db.ProjectMembers.AnyAsync(m => m.ProjectId == id && m.UserId == user.Id);
            if (!alreadyMember)
            {
                db.ProjectMembers.Add(new ProjectMember { ProjectId = id, UserId = user.Id });
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { user.Id });
        });

        api.MapDelete("/api/organizations/{orgId:int}/projects/{id:int}/members/{userId:int}", async (
            int orgId, int id, int userId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageProjects))
            {
                return Results.Forbid();
            }

            var link = await db.ProjectMembers
                .Where(m => m.ProjectId == id && m.UserId == userId
                    && m.Project.OrganizationId == orgId)
                .FirstOrDefaultAsync();
            if (link is null)
            {
                return Results.NotFound();
            }

            db.ProjectMembers.Remove(link);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static IResult? Validate(SaveProjectRequest req) =>
        string.IsNullOrWhiteSpace(req.Name)
            ? Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["A project name is required."] })
            : null;

    private static Task<bool> IsMemberAsync(TimeTrackerDbContext db, int userId, int orgId) =>
        db.UserOrganizations.AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
}
