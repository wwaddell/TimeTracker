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
                    m.Organization.CaptureLocation,
                    m.Organization.RequireProject,
                    Roles = m.Roles.Select(r => r.OrganizationRole.Name).OrderBy(n => n).ToList(),
                })
                .ToListAsync();

            var orgs = memberships
                .Select(m => new OrganizationDto(
                    m.OrganizationId, m.Name,
                    m.Roles.Count > 0 ? string.Join(", ", m.Roles) : null,
                    m.RequireTime,
                    m.CaptureLocation,
                    m.RequireProject))
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
                    f.DefaultValue,
                    f.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new EntryFieldOptionDto(o.Value, o.Label, o.Icon)).ToList()))
                .ToListAsync();

            return Results.Ok(fields);
        });

        // Recent time entries for the current user in an org (paged, optionally grouped).
        // group=day|week|month adds a GroupKey to each entry and a Groups array with full-group
        // stats (total minutes, entry count, missing-required-fields count). Week boundaries
        // follow the user's WeekStartsOn preference.
        api.MapGet("/api/organizations/{orgId:int}/time-entries",
            async (int orgId, int? page, int? pageSize, string? group,
                   TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var p = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var grouping = ParseGrouping(group);

            var baseQuery = db.TimeEntries.Where(e => e.OrganizationId == orgId && e.UserId == userId);

            // Gmail-style "of many" paging: fetch size+1 rows and use the overflow as the
            // hasMore signal. No COUNT(*) — this is the win at tens-of-thousands of entries.
            // Trade-off: caller can't jump to an arbitrary page number, only Prev/Next.
            // Materialize just the dates+ids needed for the page so we can compute group keys
            // without round-tripping the heavy projection twice.
            var pageRows = await baseQuery
                .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id)
                .Skip((p - 1) * size).Take(size + 1)
                .Select(e => new { e.Id, e.EntryDate })
                .ToListAsync();
            var hasMore = pageRows.Count > size;
            if (hasMore)
            {
                pageRows.RemoveAt(pageRows.Count - 1); // drop the probe row
            }
            var pageIds = pageRows.Select(r => r.Id).ToList();

            // For week grouping we need the user's preferred week start.
            var weekStart = grouping == Grouping.Week
                ? await db.Users.Where(u => u.Id == userId).Select(u => u.WeekStartsOn).FirstAsync()
                : DayOfWeek.Sunday;

            // Page items in their original order, with group key attached.
            var items = await baseQuery
                .Where(e => pageIds.Contains(e.Id))
                .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id)
                .Select(e => new
                {
                    Dto = new TimeEntryDto(
                        e.Id, e.EntryDate, e.StartTime, e.DurationMinutes, e.Note,
                        e.TaskId,
                        e.Task != null ? e.Task.Title : null,
                        e.ProjectId,
                        e.Project != null ? e.Project.Name : null,
                        e.CreatedUtc,
                        e.Source,
                        e.SourceIsRecurring,
                        "", // GroupKey filled in below
                        e.Attributes.Select(a => new TimeEntryAttributeDto(
                            a.TimeEntryFieldId, a.TimeEntryField.Label, a.Value)).ToList()),
                    EntryDate = e.EntryDate,
                })
                .ToListAsync();

            var withKeys = items
                .Select(x => x.Dto with { GroupKey = GroupKeyFor(x.EntryDate, grouping, weekStart) })
                .ToList();

            // No grouping → just return the page.
            if (grouping == Grouping.None)
            {
                return Results.Ok(new TimeEntriesPage(withKeys, p, size, hasMore, []));
            }

            // No entries on this page → nothing to group. Without this guard, ranges.Min()
            // below throws on the empty sequence (500 when grouping is on and the user has
            // no entries yet).
            if (withKeys.Count == 0)
            {
                return Results.Ok(new TimeEntriesPage(withKeys, p, size, hasMore, []));
            }

            // Compute the date ranges covered by the visible groups, then load ALL entries
            // in those ranges so totals reflect the full group (not just the page slice).
            var groupKeys = withKeys.Select(e => e.GroupKey).Distinct().ToList();
            var ranges = groupKeys
                .Select(k => GroupRange(k, grouping, weekStart))
                .ToList();
            var minDate = ranges.Min(r => r.start);
            var maxDate = ranges.Max(r => r.endInclusive);

            // Required-field ids visible to this user in this org (org-wide or matching a held role).
            var roleIds = await currentUser.GetRoleIdsAsync(orgId);
            var requiredFieldIds = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive && f.IsRequired
                    && (f.OrganizationRoleId == null || roleIds.Contains(f.OrganizationRoleId.Value)))
                .Select(f => f.Id)
                .ToListAsync();

            // Pull every entry in the visible groups' date span, plus its filled attribute ids
            // (only required ones — we just need to check coverage).
            var requiredSet = requiredFieldIds.ToHashSet();
            var groupEntries = await baseQuery
                .Where(e => e.EntryDate >= minDate && e.EntryDate <= maxDate)
                .Select(e => new
                {
                    e.Id,
                    e.EntryDate,
                    e.DurationMinutes,
                    FilledRequiredCount = e.Attributes
                        .Count(a => requiredSet.Contains(a.TimeEntryFieldId)
                            && a.Value != null && a.Value.Trim().Length > 0),
                })
                .ToListAsync();

            var groups = groupEntries
                .GroupBy(e => GroupKeyFor(e.EntryDate, grouping, weekStart))
                .Where(g => groupKeys.Contains(g.Key))
                .Select(g => new TimeEntryGroupDto(
                    g.Key,
                    GroupLabel(g.Key, grouping, weekStart),
                    g.Sum(e => e.DurationMinutes ?? 0),
                    g.Count(),
                    g.Count(e => e.FilledRequiredCount < requiredSet.Count)))
                // Newest group first (matches the entry order).
                .OrderByDescending(g => g.Key)
                .ToList();

            return Results.Ok(new TimeEntriesPage(withKeys, p, size, hasMore, groups));
        });

        static Grouping ParseGrouping(string? g) => g?.ToLowerInvariant() switch
        {
            "day" => Grouping.Day,
            "week" => Grouping.Week,
            "month" => Grouping.Month,
            _ => Grouping.None,
        };

        // Group key (used to bucket entries) — stable, sortable.
        // Day: "yyyy-MM-dd"   Week: start-of-week "yyyy-MM-dd"   Month: "yyyy-MM"
        static string GroupKeyFor(DateOnly date, Grouping g, DayOfWeek weekStart) => g switch
        {
            Grouping.Day => date.ToString("yyyy-MM-dd"),
            Grouping.Week => WeekStart(date, weekStart).ToString("yyyy-MM-dd"),
            Grouping.Month => date.ToString("yyyy-MM"),
            _ => "",
        };

        // Inclusive [start, end] date range covered by the group key (so we know which entries to sum).
        static (DateOnly start, DateOnly endInclusive) GroupRange(string key, Grouping g, DayOfWeek weekStart) => g switch
        {
            Grouping.Day => (DateOnly.Parse(key), DateOnly.Parse(key)),
            Grouping.Week => (DateOnly.Parse(key), DateOnly.Parse(key).AddDays(6)),
            Grouping.Month => MonthRange(key),
            _ => (DateOnly.MinValue, DateOnly.MaxValue),
        };

        static (DateOnly, DateOnly) MonthRange(string key) // "yyyy-MM"
        {
            var year = int.Parse(key[..4]);
            var month = int.Parse(key[5..]);
            var first = new DateOnly(year, month, 1);
            return (first, first.AddMonths(1).AddDays(-1));
        }

        static DateOnly WeekStart(DateOnly date, DayOfWeek weekStart)
        {
            var diff = ((int)date.DayOfWeek - (int)weekStart + 7) % 7;
            return date.AddDays(-diff);
        }

        // Human-readable header label per grouping.
        static string GroupLabel(string key, Grouping g, DayOfWeek weekStart) => g switch
        {
            Grouping.Day => DateOnly.Parse(key).ToString("ddd, MMM d, yyyy"),
            Grouping.Week => $"Week of {DateOnly.Parse(key):MMM d, yyyy}",
            Grouping.Month => DateOnly.Parse(key + "-01").ToString("MMMM yyyy"),
            _ => key,
        };

        // Create a time entry.
        api.MapPost("/api/organizations/{orgId:int}/time-entries",
            async (int orgId, CreateTimeEntryRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (ValidateNote(request.Note) is { } problem)
            {
                return problem;
            }

            var userId = await currentUser.GetUserIdAsync();
            var membership = await db.UserOrganizations
                .Include(m => m.Organization)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == orgId);
            if (membership is null)
            {
                return Results.Forbid();
            }

            // Org policy: if RequireProject is on, the FK is mandatory.
            if (membership.Organization.RequireProject && request.ProjectId is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["projectId"] = ["A project is required for this organization."],
                });
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
                // Hidden context: stored verbatim from the client. Truncate timezone defensively.
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationAccuracy = request.LocationAccuracy,
                Timezone = string.IsNullOrWhiteSpace(request.Timezone)
                    ? null
                    : request.Timezone.Trim()[..Math.Min(request.Timezone.Trim().Length, 64)],
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
            if (ValidateNote(request.Note) is { } problem)
            {
                return problem;
            }

            var userId = await currentUser.GetUserIdAsync();
            var entry = await db.TimeEntries
                .Include(e => e.Attributes)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == orgId && e.UserId == userId);
            if (entry is null)
            {
                return Results.NotFound();
            }

            // Org policy: if RequireProject is on, the FK is mandatory on saves too.
            var requireProject = await db.Organizations
                .Where(o => o.Id == orgId).Select(o => o.RequireProject).FirstAsync();
            if (requireProject && request.ProjectId is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["projectId"] = ["A project is required for this organization."],
                });
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

    // Note length cap matches the EF configuration (TimeEntryConfiguration). Validated here
    // so callers see a friendly 400 ProblemDetails rather than a SqlException at save time.
    private const int NoteMaxLength = 2000;

    private static IResult? ValidateNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["note"] = ["A note describing what was completed is required."],
            });
        }
        if (note.Length > NoteMaxLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["note"] = [$"Note must be {NoteMaxLength} characters or fewer."],
            });
        }
        return null;
    }

    private enum Grouping { None, Day, Week, Month }
}
