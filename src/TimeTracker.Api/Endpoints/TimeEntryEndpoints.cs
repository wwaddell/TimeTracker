using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts;
using TimeTracker.Contracts.TimeEntries;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class TimeEntryEndpoints
{
    public static void MapTimeTrackerEndpoints(this IEndpointRouteBuilder app)
    {
        // All time-logging endpoints require an authenticated user.
        var api = app.MapGroup("").RequireAuthorization();

        // Organizations the current user belongs to.
        api.MapGet("/api/organizations", async (TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var memberships = await db.UserOrganizations
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.IsDefault).ThenBy(m => m.Organization.Name)
                .Select(m => new
                {
                    m.OrganizationId,
                    m.Organization.Name,
                    m.Organization.RequireTime,
                    Roles = m.Roles.Select(r => r.OrganizationRole.Name).OrderBy(n => n).ToList(),
                })
                .ToListAsync();

            var orgs = memberships
                .Select(m => new OrganizationDto(
                    m.OrganizationId, m.Name,
                    m.Roles.Count > 0 ? string.Join(", ", m.Roles) : null,
                    m.RequireTime))
                .ToList();

            return Results.Ok(orgs);
        });

        // Configurable field definitions for an org (and the user's role within it).
        api.MapGet("/api/organizations/{orgId:int}/entry-fields", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var isMember = await db.UserOrganizations
                .AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (!isMember)
            {
                return Results.Forbid();
            }

            // A field is shown if it's org-wide (no role) or scoped to one of the user's roles.
            var roleIds = await currentUser.GetRoleIdsAsync(orgId);
            var fields = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive
                    && (f.OrganizationRoleId == null || roleIds.Contains(f.OrganizationRoleId.Value)))
                .OrderBy(f => f.SortOrder)
                .Select(f => new EntryFieldDto(
                    f.Id, f.FieldKey, f.Label, f.DataType, f.IsRequired, f.SortOrder,
                    f.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new EntryFieldOptionDto(o.Value, o.Label, o.Icon)).ToList()))
                .ToListAsync();

            return Results.Ok(fields);
        });

        // Recent time entries for the current user in an org (paged).
        api.MapGet("/api/organizations/{orgId:int}/time-entries",
            async (int orgId, int? page, int? pageSize, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var p = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);

            var query = db.TimeEntries.Where(e => e.OrganizationId == orgId && e.UserId == userId);
            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id)
                .Skip((p - 1) * size).Take(size)
                .Select(e => new TimeEntryDto(
                    e.Id, e.EntryDate, e.StartTime, e.DurationMinutes, e.Note,
                    e.TaskId,
                    e.Task != null ? e.Task.Title : null,
                    e.ProjectId,
                    e.Project != null ? e.Project.Name : null,
                    e.CreatedUtc,
                    e.Source,
                    e.SourceIsRecurring,
                    e.Attributes.Select(a => new TimeEntryAttributeDto(
                        a.TimeEntryFieldId, a.TimeEntryField.Label, a.Value)).ToList()))
                .ToListAsync();

            return Results.Ok(new PagedResult<TimeEntryDto>(items, p, size, total));
        });

        // Create a time entry.
        api.MapPost("/api/organizations/{orgId:int}/time-entries",
            async (int orgId, CreateTimeEntryRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["note"] = ["A note describing what was completed is required."],
                });
            }

            var userId = await currentUser.GetUserIdAsync();
            var membership = await db.UserOrganizations
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.Forbid();
            }

            // Only accept attribute values for fields that belong to this org.
            var validFieldIds = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive)
                .Select(f => f.Id)
                .ToListAsync();

            var entry = new TimeEntry
            {
                UserId = userId,
                OrganizationId = orgId,
                TaskId = request.TaskId,
                ProjectId = request.ProjectId,
                EntryDate = request.EntryDate == default ? DateOnly.FromDateTime(DateTime.Now) : request.EntryDate,
                StartTime = request.StartTime,
                DurationMinutes = request.DurationMinutes,
                Note = request.Note.Trim(),
                CreatedUtc = DateTime.UtcNow,
            };

            foreach (var (fieldId, value) in request.Attributes)
            {
                if (!validFieldIds.Contains(fieldId) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                entry.Attributes.Add(new TimeEntryAttribute { TimeEntryFieldId = fieldId, Value = value });
            }

            db.TimeEntries.Add(entry);
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizations/{orgId}/time-entries/{entry.Id}", new { entry.Id });
        });

        // Update a time entry (core fields + attributes).
        api.MapPut("/api/organizations/{orgId:int}/time-entries/{id:long}",
            async (int orgId, long id, CreateTimeEntryRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["note"] = ["A note describing what was completed is required."],
                });
            }

            var userId = await currentUser.GetUserIdAsync();
            var entry = await db.TimeEntries
                .Include(e => e.Attributes)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == orgId && e.UserId == userId);
            if (entry is null)
            {
                return Results.NotFound();
            }

            entry.TaskId = request.TaskId;
            entry.ProjectId = request.ProjectId;
            entry.EntryDate = request.EntryDate == default ? entry.EntryDate : request.EntryDate;
            entry.StartTime = request.StartTime;
            entry.DurationMinutes = request.DurationMinutes;
            entry.Note = request.Note.Trim();
            entry.ModifiedUtc = DateTime.UtcNow;

            var validFieldIds = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive)
                .Select(f => f.Id)
                .ToListAsync();

            db.TimeEntryAttributes.RemoveRange(entry.Attributes);
            entry.Attributes.Clear();
            foreach (var (fieldId, value) in request.Attributes)
            {
                if (!validFieldIds.Contains(fieldId) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                entry.Attributes.Add(new TimeEntryAttribute { TimeEntryFieldId = fieldId, Value = value });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { entry.Id });
        });

        // Soft-delete a time entry: it disappears from lists but the row is preserved
        // (and an imported occurrence becomes available to import again).
        api.MapDelete("/api/organizations/{orgId:int}/time-entries/{id:long}",
            async (int orgId, long id, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var entry = await db.TimeEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == orgId && e.UserId == userId);
            if (entry is null)
            {
                return Results.NotFound();
            }

            entry.DeletedUtc = DateTime.UtcNow;
            entry.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
