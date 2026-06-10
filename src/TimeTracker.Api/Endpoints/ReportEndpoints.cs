using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Reports;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

/// <summary>
/// Org-wide reports, all gated on <see cref="OrgRight.ViewReports"/>. Each endpoint
/// returns flat display-ready rows (see Contracts/Reports) — the client renders them in
/// a generic table and exports them verbatim, so there's a single code path for both.
/// Date ranges are inclusive on both ends and interpreted against EntryDate (a DateOnly,
/// no timezone math) except tasks-completed, which compares against the audit log's UTC
/// change timestamps.
/// </summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/organizations/{orgId:int}/reports").RequireAuthorization();

        // Every time entry in the range, optionally filtered to a project and/or person.
        grp.MapGet("/time-detail", async (
            int orgId, DateOnly from, DateOnly to, int? projectId, int? userId,
            TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var query = db.TimeEntries.AsNoTracking()
                .Where(e => e.OrganizationId == orgId && e.EntryDate >= from && e.EntryDate <= to);
            if (projectId is { } pid)
            {
                query = query.Where(e => e.ProjectId == pid);
            }
            if (userId is { } uid)
            {
                query = query.Where(e => e.UserId == uid);
            }

            var rows = await query
                .OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime)
                .Select(e => new TimeDetailRow(
                    e.EntryDate,
                    e.User.DisplayName,
                    e.Project != null ? e.Project.Name : null,
                    e.Task != null ? e.Task.Title : null,
                    e.Note,
                    e.DurationMinutes))
                .ToListAsync();

            return Results.Ok(rows);
        });

        // Total time per project. Unassigned entries group under "(No project)".
        grp.MapGet("/time-by-project", async (
            int orgId, DateOnly from, DateOnly to, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var rows = await db.TimeEntries.AsNoTracking()
                .Where(e => e.OrganizationId == orgId && e.EntryDate >= from && e.EntryDate <= to)
                .GroupBy(e => e.Project != null ? e.Project.Name : "(No project)")
                .Select(g => new
                {
                    Project = g.Key,
                    Entries = g.Count(),
                    Minutes = g.Sum(e => e.DurationMinutes ?? 0),
                })
                .OrderByDescending(x => x.Minutes)
                .ToListAsync();

            return Results.Ok(rows.Select(x =>
                new TimeByProjectRow(x.Project, x.Entries, x.Minutes, ToHours(x.Minutes))).ToList());
        });

        // Total time per member.
        grp.MapGet("/time-by-person", async (
            int orgId, DateOnly from, DateOnly to, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var rows = await db.TimeEntries.AsNoTracking()
                .Where(e => e.OrganizationId == orgId && e.EntryDate >= from && e.EntryDate <= to)
                .GroupBy(e => e.User.DisplayName)
                .Select(g => new
                {
                    Person = g.Key,
                    Entries = g.Count(),
                    Minutes = g.Sum(e => e.DurationMinutes ?? 0),
                })
                .OrderByDescending(x => x.Minutes)
                .ToListAsync();

            return Results.Ok(rows.Select(x =>
                new TimeByPersonRow(x.Person, x.Entries, x.Minutes, ToHours(x.Minutes))).ToList());
        });

        // Total time per day/week/month bucket. Week start honors the requesting user's
        // preference (same as Log Time's grouping).
        grp.MapGet("/time-by-period", async (
            int orgId, DateOnly from, DateOnly to, string? bucket,
            TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var requesterId = await currentUser.GetUserIdAsync();
            var weekStart = await db.Users.AsNoTracking()
                .Where(u => u.Id == requesterId)
                .Select(u => u.WeekStartsOn)
                .FirstOrDefaultAsync();

            // Bucketing happens in memory: the period-key math (week start arithmetic)
            // doesn't translate to SQL, and a date-bounded slice of entries is small.
            var entries = await db.TimeEntries.AsNoTracking()
                .Where(e => e.OrganizationId == orgId && e.EntryDate >= from && e.EntryDate <= to)
                .Select(e => new { e.EntryDate, e.DurationMinutes })
                .ToListAsync();

            var b = bucket?.ToLowerInvariant() switch
            {
                "week" => "week",
                "month" => "month",
                _ => "day",
            };

            var rows = entries
                .GroupBy(e => PeriodKey(e.EntryDate, b, weekStart))
                .Select(g => new
                {
                    Period = g.Key,
                    Entries = g.Count(),
                    Minutes = g.Sum(e => e.DurationMinutes ?? 0),
                })
                .OrderBy(x => x.Period)
                .Select(x => new TimeByPeriodRow(x.Period, x.Entries, x.Minutes, ToHours(x.Minutes)))
                .ToList();

            return Results.Ok(rows);
        });

        // Tasks completed in the range. "Completed" = the audit history recorded an
        // IsComplete -> True flip inside the range AND the task is still complete now
        // (a task completed then reopened doesn't count). ActualHours sums the task's
        // time entries (all time, not just the range — effort belongs to the task).
        grp.MapGet("/tasks-completed", async (
            int orgId, DateOnly from, DateOnly to, int? assigneeId,
            TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toUtcExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var completions = db.TaskHistories.AsNoTracking()
                .Where(h => h.FieldName == "IsComplete" && h.NewValue == "True"
                    && h.ChangedUtc >= fromUtc && h.ChangedUtc < toUtcExclusive
                    && h.Task.OrganizationId == orgId && h.Task.IsComplete);

            var taskQuery = completions
                .GroupBy(h => h.TaskId)
                .Select(g => new { TaskId = g.Key, CompletedUtc = g.Max(h => h.ChangedUtc) });

            var rows = await taskQuery
                .Join(db.Tasks.AsNoTracking(), c => c.TaskId, t => t.Id, (c, t) => new { c, t })
                .Where(x => assigneeId == null || x.t.AssignedToUserId == assigneeId)
                .Select(x => new
                {
                    x.t.Title,
                    AssignedTo = x.t.AssignedTo.DisplayName,
                    Project = x.t.ProjectEntity != null ? x.t.ProjectEntity.Name : null,
                    x.c.CompletedUtc,
                    x.t.EstimatedHours,
                    ActualMinutes = x.t.TimeEntries.Sum(e => e.DurationMinutes ?? 0),
                })
                .OrderBy(x => x.CompletedUtc)
                .ToListAsync();

            return Results.Ok(rows.Select(x => new TaskCompletedRow(
                x.Title, x.AssignedTo, x.Project, x.CompletedUtc,
                x.EstimatedHours, ToHours(x.ActualMinutes))).ToList());
        });

        // Snapshot of currently open tasks. Age counts from creation; Overdue compares
        // the due date to today (server date).
        grp.MapGet("/open-tasks", async (
            int orgId, int? assigneeId, int? projectId, bool? overdueOnly,
            TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = db.Tasks.AsNoTracking()
                .Where(t => t.OrganizationId == orgId && !t.IsComplete);
            if (assigneeId is { } aid)
            {
                query = query.Where(t => t.AssignedToUserId == aid);
            }
            if (projectId is { } pid)
            {
                query = query.Where(t => t.ProjectId == pid);
            }
            if (overdueOnly == true)
            {
                query = query.Where(t => t.DueDate != null && t.DueDate < today);
            }

            var tasks = await query
                .OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ThenBy(t => t.Title)
                .Select(t => new
                {
                    t.Title,
                    AssignedTo = t.AssignedTo.DisplayName,
                    Project = t.ProjectEntity != null ? t.ProjectEntity.Name : null,
                    t.Priority,
                    t.DueDate,
                    t.PercentComplete,
                    t.CreatedUtc,
                })
                .ToListAsync();

            var rows = tasks.Select(t => new OpenTaskRow(
                t.Title,
                t.AssignedTo,
                t.Project,
                t.Priority?.ToString(),
                t.DueDate,
                t.PercentComplete,
                Math.Max(0, today.DayNumber - DateOnly.FromDateTime(t.CreatedUtc).DayNumber),
                t.DueDate is { } due && due < today)).ToList();

            return Results.Ok(rows);
        });

        // Entries in the range missing required configurable fields. "Required for this
        // entry" = the field is active + required AND is org-wide or scoped to a role the
        // entry's author holds — mirroring what the entry form shows that user.
        grp.MapGet("/incomplete-entries", async (
            int orgId, DateOnly from, DateOnly to, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ViewReports))
            {
                return Results.Forbid();
            }

            var requiredFields = await db.TimeEntryFields.AsNoTracking()
                .Where(f => f.OrganizationId == orgId && f.IsActive && f.IsRequired)
                .Select(f => new { f.Id, f.Label, f.OrganizationRoleId })
                .ToListAsync();
            if (requiredFields.Count == 0)
            {
                return Results.Ok(new List<IncompleteEntryRow>());
            }

            // userId -> role ids held in this org, for resolving role-scoped fields.
            var roleHoldings = (await db.UserOrganizationRoles.AsNoTracking()
                    .Where(r => r.UserOrganization.OrganizationId == orgId)
                    .Select(r => new { r.UserOrganization.UserId, r.OrganizationRoleId })
                    .ToListAsync())
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.OrganizationRoleId).ToHashSet());

            var entries = await db.TimeEntries.AsNoTracking()
                .Where(e => e.OrganizationId == orgId && e.EntryDate >= from && e.EntryDate <= to)
                .Select(e => new
                {
                    e.EntryDate,
                    e.Note,
                    e.UserId,
                    Person = e.User.DisplayName,
                    FilledFieldIds = e.Attributes
                        .Where(a => a.Value != null && a.Value.Trim().Length > 0)
                        .Select(a => a.TimeEntryFieldId)
                        .ToList(),
                })
                .ToListAsync();

            var rows = new List<IncompleteEntryRow>();
            foreach (var e in entries)
            {
                var heldRoles = roleHoldings.GetValueOrDefault(e.UserId);
                var missing = requiredFields
                    .Where(f => f.OrganizationRoleId == null
                        || (heldRoles is not null && heldRoles.Contains(f.OrganizationRoleId.Value)))
                    .Where(f => !e.FilledFieldIds.Contains(f.Id))
                    .Select(f => f.Label)
                    .ToList();
                if (missing.Count > 0)
                {
                    rows.Add(new IncompleteEntryRow(e.EntryDate, e.Person, e.Note, string.Join(", ", missing)));
                }
            }

            return Results.Ok(rows.OrderBy(r => r.Date).ToList());
        });
    }

    private static decimal ToHours(int minutes) => Math.Round(minutes / 60m, 2);

    // Mirror of Log Time's grouping keys so period labels match what users see there.
    private static string PeriodKey(DateOnly date, string bucket, DayOfWeek weekStart) => bucket switch
    {
        "week" => $"Week of {WeekStartOf(date, weekStart):yyyy-MM-dd}",
        "month" => date.ToString("yyyy-MM"),
        _ => date.ToString("yyyy-MM-dd"),
    };

    private static DateOnly WeekStartOf(DateOnly date, DayOfWeek weekStart)
    {
        var diff = ((int)date.DayOfWeek - (int)weekStart + 7) % 7;
        return date.AddDays(-diff);
    }
}
