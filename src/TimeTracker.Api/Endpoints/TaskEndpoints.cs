using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("").RequireAuthorization();

        // List the current user's tasks within an org.
        // ?scope=mine (default): tasks assigned to me; assigned-by-me: tasks I created that are
        // now assigned to someone else; all: either assigned to me OR created by me.
        api.MapGet("/api/organizations/{orgId:int}/tasks", async (
            int orgId, string? scope, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var baseQuery = db.Tasks.Where(t => t.OrganizationId == orgId);
            var filtered = scope?.ToLowerInvariant() switch
            {
                "assigned-by-me" => baseQuery.Where(t => t.UserId == userId && t.AssignedToUserId != userId),
                "all"            => baseQuery.Where(t => t.AssignedToUserId == userId || t.UserId == userId),
                _                => baseQuery.Where(t => t.AssignedToUserId == userId), // "mine" (default)
            };

            var tasks = await filtered
                .OrderBy(t => t.IsComplete).ThenByDescending(t => t.Id)
                .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsComplete,
                    t.EstimatedHours, t.PercentComplete, t.PercentBeforeComplete,
                    t.Priority, t.DueDate, t.Project,
                    t.ProjectId, t.ProjectEntity != null ? t.ProjectEntity.Name : null,
                    t.ReferenceCode, t.ExternalUrl,
                    t.AssignedToUserId, t.AssignedTo.DisplayName,
                    t.CreatedUtc))
                .ToListAsync();

            return Results.Ok(tasks);
        });

        // Create a task.
        api.MapPost("/api/organizations/{orgId:int}/tasks",
            async (int orgId, SaveTaskRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            if (Validate(request) is { } problem)
            {
                return problem;
            }

            // Assignee: 0 means "default to the creator"; otherwise must be a member of this org.
            var assignedTo = request.AssignedToUserId == 0 ? userId : request.AssignedToUserId;
            if (assignedTo != userId && !await IsMemberAsync(db, assignedTo, orgId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["assignedToUserId"] = ["The selected assignee isn't a member of this organization."],
                });
            }

            var task = new TaskItem
            {
                UserId = userId,
                AssignedToUserId = assignedTo,
                OrganizationId = orgId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsComplete = request.IsComplete,
                EstimatedHours = request.EstimatedHours,
                PercentComplete = request.IsComplete ? 100 : request.PercentComplete,
                PercentBeforeComplete = request.IsComplete ? request.PercentBeforeComplete : null,
                Priority = request.Priority,
                DueDate = request.DueDate,
                Project = string.IsNullOrWhiteSpace(request.Project) ? null : request.Project.Trim(),
                ProjectId = request.ProjectId,
                ReferenceCode = string.IsNullOrWhiteSpace(request.ReferenceCode) ? null : request.ReferenceCode.Trim(),
                ExternalUrl = string.IsNullOrWhiteSpace(request.ExternalUrl) ? null : request.ExternalUrl.Trim(),
                CreatedUtc = DateTime.UtcNow,
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizations/{orgId}/tasks/{task.Id}", new { task.Id });
        });

        // Update a task.
        api.MapPut("/api/organizations/{orgId:int}/tasks/{id:int}",
            async (int orgId, int id, SaveTaskRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            // Editable by the creator or the current assignee.
            var task = await db.Tasks.FirstOrDefaultAsync(t =>
                t.Id == id && t.OrganizationId == orgId
                && (t.UserId == userId || t.AssignedToUserId == userId));
            if (task is null)
            {
                return Results.NotFound();
            }

            if (Validate(request) is { } problem)
            {
                return problem;
            }

            // Assignee change: 0 means "leave unchanged"; otherwise validate org membership.
            if (request.AssignedToUserId != 0 && request.AssignedToUserId != task.AssignedToUserId)
            {
                if (!await IsMemberAsync(db, request.AssignedToUserId, orgId))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["assignedToUserId"] = ["The selected assignee isn't a member of this organization."],
                    });
                }
                task.AssignedToUserId = request.AssignedToUserId;
            }

            // Authoritative save: persist exactly what the caller sends, normalizing so a
            // complete task is always 100% (and the prior % is only retained while complete).
            task.Title = request.Title.Trim();
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            task.EstimatedHours = request.EstimatedHours;
            task.IsComplete = request.IsComplete;
            task.PercentComplete = request.IsComplete ? 100 : request.PercentComplete;
            task.PercentBeforeComplete = request.IsComplete ? request.PercentBeforeComplete : null;
            task.Priority = request.Priority;
            task.DueDate = request.DueDate;
            task.Project = string.IsNullOrWhiteSpace(request.Project) ? null : request.Project.Trim();
            task.ProjectId = request.ProjectId;
            task.ReferenceCode = string.IsNullOrWhiteSpace(request.ReferenceCode) ? null : request.ReferenceCode.Trim();
            task.ExternalUrl = string.IsNullOrWhiteSpace(request.ExternalUrl) ? null : request.ExternalUrl.Trim();
            task.ModifiedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { task.Id });
        });

        // Soft-delete a task: it disappears from lists but the row (and any linked entries) is kept.
        api.MapDelete("/api/organizations/{orgId:int}/tasks/{id:int}",
            async (int orgId, int id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var task = await db.Tasks.FirstOrDefaultAsync(t =>
                t.Id == id && t.OrganizationId == orgId
                && (t.UserId == userId || t.AssignedToUserId == userId));
            if (task is null)
            {
                return Results.NotFound();
            }

            task.DeletedUtc = DateTime.UtcNow;
            task.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // History: every create/edit/soft-delete on a task is appended automatically by the
        // DbContext's SaveChanges override (see TimeTrackerDbContext.SaveChangesAsync). This
        // endpoint just reads them newest-first. Same visibility rule as PUT/DELETE: only the
        // creator or current assignee can see the trail. IgnoreQueryFilters so a soft-deleted
        // task's history is still readable (you may want to see WHO deleted it).
        api.MapGet("/api/organizations/{orgId:int}/tasks/{id:int}/history", async (
            int orgId, int id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var task = await db.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t =>
                t.Id == id && t.OrganizationId == orgId
                && (t.UserId == userId || t.AssignedToUserId == userId));
            if (task is null)
            {
                return Results.NotFound();
            }

            var rows = await db.TaskHistories
                .Where(h => h.TaskId == id)
                .OrderByDescending(h => h.ChangedUtc).ThenByDescending(h => h.Id)
                .Select(h => new TaskHistoryDto(
                    h.Id,
                    h.ChangedUtc,
                    h.ChangedByUserId,
                    h.ChangedBy != null ? h.ChangedBy.DisplayName : null,
                    h.ChangeType.ToString(),
                    h.FieldName,
                    h.OldValue,
                    h.NewValue))
                .ToListAsync();

            return Results.Ok(rows);
        });
    }

    private static IResult? Validate(SaveTaskRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            errors["title"] = ["A title is required."];
        }
        if (req.PercentComplete is < 0 or > 100)
        {
            errors["percentComplete"] = ["Percent complete must be between 0 and 100."];
        }
        if (req.EstimatedHours is < 0)
        {
            errors["estimatedHours"] = ["Estimated hours cannot be negative."];
        }
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static Task<bool> IsMemberAsync(TimeTrackerDbContext db, int userId, int orgId) =>
        db.UserOrganizations.AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
}
