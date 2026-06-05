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
        api.MapGet("/api/organizations/{orgId:int}/tasks", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var tasks = await db.Tasks
                .Where(t => t.UserId == userId && t.OrganizationId == orgId)
                .OrderBy(t => t.IsComplete).ThenByDescending(t => t.Id)
                .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsComplete,
                    t.EstimatedHours, t.PercentComplete, t.PercentBeforeComplete,
                    t.Priority, t.DueDate, t.CreatedUtc))
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

            var task = new TaskItem
            {
                UserId = userId,
                OrganizationId = orgId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsComplete = request.IsComplete,
                EstimatedHours = request.EstimatedHours,
                PercentComplete = request.IsComplete ? 100 : request.PercentComplete,
                PercentBeforeComplete = request.IsComplete ? request.PercentBeforeComplete : null,
                Priority = request.Priority,
                DueDate = request.DueDate,
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
            var task = await db.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId && t.UserId == userId);
            if (task is null)
            {
                return Results.NotFound();
            }

            if (Validate(request) is { } problem)
            {
                return problem;
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
            task.ModifiedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { task.Id });
        });

        // Soft-delete a task: it disappears from lists but the row (and any linked entries) is kept.
        api.MapDelete("/api/organizations/{orgId:int}/tasks/{id:int}",
            async (int orgId, int id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var task = await db.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId && t.UserId == userId);
            if (task is null)
            {
                return Results.NotFound();
            }

            task.DeletedUtc = DateTime.UtcNow;
            task.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
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
