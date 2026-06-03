using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
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
            var orgs = await db.UserOrganizations
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.IsDefault).ThenBy(m => m.Organization.Name)
                .Select(m => new OrganizationDto(
                    m.OrganizationId,
                    m.Organization.Name,
                    m.OrganizationRole != null ? m.OrganizationRole.Name : null))
                .ToListAsync();

            return Results.Ok(orgs);
        });

        // Configurable field definitions for an org (and the user's role within it).
        api.MapGet("/api/organizations/{orgId:int}/entry-fields", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var membership = await db.UserOrganizations
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var fields = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive
                    && (f.OrganizationRoleId == null || f.OrganizationRoleId == membership.OrganizationRoleId))
                .OrderBy(f => f.SortOrder)
                .Select(f => new EntryFieldDto(
                    f.Id, f.FieldKey, f.Label, f.DataType, f.IsRequired, f.SortOrder,
                    f.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new EntryFieldOptionDto(o.Value, o.Label)).ToList()))
                .ToListAsync();

            return Results.Ok(fields);
        });

        // Recent time entries for the current user in an org.
        api.MapGet("/api/organizations/{orgId:int}/time-entries", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var entries = await db.TimeEntries
                .Where(e => e.OrganizationId == orgId && e.UserId == userId)
                .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id)
                .Take(50)
                .Select(e => new TimeEntryDto(
                    e.Id, e.EntryDate, e.StartTime, e.DurationMinutes, e.Note,
                    e.Task != null ? e.Task.Title : null,
                    e.CreatedUtc,
                    e.Attributes.Select(a => new TimeEntryAttributeDto(
                        a.TimeEntryFieldId, a.TimeEntryField.Label, a.Value)).ToList()))
                .ToListAsync();

            return Results.Ok(entries);
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
    }
}
