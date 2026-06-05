using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeTracker.Api.Auth;
using TimeTracker.Api.Calendar;
using TimeTracker.Contracts.Calendar;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class CalendarEndpoints
{
    public static void MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("").RequireAuthorization();

        // Preview meetings to import for a date range. Any member may import their own calendar.
        api.MapPost("/api/organizations/{orgId:int}/calendar/preview", async (
            int orgId, CalendarPreviewRequest request, TimeTrackerDbContext db,
            ICurrentUser currentUser, ICalendarSource graph, ICalendarTokenProvider tokens, IWebHostEnvironment env) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var (fromUtc, toUtc) = ToUtcWindow(request.From, request.To);
            var pasted = request.AccessToken?.Trim();

            // Token source, in order: explicit "FAKE" sentinel → sample data; a pasted dev token →
            // use directly; otherwise the user's stored Outlook connection; otherwise (dev) fall
            // back to sample data, or prompt to connect.
            IReadOnlyList<CalendarEvent> events;
            var useSample = false;
            try
            {
                string? token;
                if (pasted == DevCalendarSamples.Sentinel)
                {
                    token = null;
                    useSample = true;
                }
                else if (!string.IsNullOrEmpty(pasted))
                {
                    token = pasted;
                }
                else
                {
                    token = await tokens.GetAccessTokenAsync(userId);
                    if (token is null)
                    {
                        if (env.IsDevelopment())
                        {
                            useSample = true;
                        }
                        else
                        {
                            return Results.ValidationProblem(new Dictionary<string, string[]>
                            {
                                ["accessToken"] = ["Connect Outlook to import meetings."],
                            });
                        }
                    }
                }

                events = useSample
                    ? DevCalendarSamples.Build(fromUtc, toUtc)
                    : await graph.GetEventsAsync(token!, fromUtc, toUtc);
            }
            catch (CalendarSourceException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["accessToken"] = [ex.Message],
                });
            }

            var uids = events.Select(e => e.SeriesUid).Distinct().ToList();

            // Existing imported occurrences (for dedup) + remembered series tags (for suggestions).
            var existing = await db.TimeEntries
                .Where(e => e.OrganizationId == orgId && e.UserId == userId
                    && e.SourceSeriesUid != null && uids.Contains(e.SourceSeriesUid))
                .Select(e => new { e.SourceSeriesUid, e.SourceOccurrenceStartUtc })
                .ToListAsync();
            var importedKeys = existing
                .Select(e => OccurrenceKey(e.SourceSeriesUid, e.SourceOccurrenceStartUtc))
                .ToHashSet();

            var tags = await db.CalendarSeriesTags
                .Where(t => t.OrganizationId == orgId && t.UserId == userId && uids.Contains(t.SeriesUid))
                .Select(t => new
                {
                    t.SeriesUid,
                    t.TaskId,
                    Attributes = t.Attributes.Select(a => new CalendarAttributeValue(a.TimeEntryFieldId, a.Value)).ToList(),
                })
                .ToDictionaryAsync(t => t.SeriesUid);

            var meetings = events.Select(e =>
            {
                var local = e.StartUtc.ToLocalTime();
                var occurrenceStartUtc = e.StartUtc.UtcDateTime;
                var already = importedKeys.Contains(OccurrenceKey(e.SeriesUid, occurrenceStartUtc));
                tags.TryGetValue(e.SeriesUid, out var tag);

                return new CalendarMeetingDto
                {
                    SeriesUid = e.SeriesUid,
                    EventId = e.EventId,
                    OccurrenceStartUtc = occurrenceStartUtc,
                    Subject = e.Subject,
                    EntryDate = DateOnly.FromDateTime(local.DateTime),
                    StartTime = TimeOnly.FromDateTime(local.DateTime),
                    DurationMinutes = Math.Max(0, (int)(e.EndUtc - e.StartUtc).TotalMinutes),
                    IsRecurring = e.IsRecurring,
                    IsAllDay = e.IsAllDay,
                    ShowAs = e.ShowAs,
                    OrganizerName = e.OrganizerName,
                    AlreadyImported = already,
                    // Default-select real, busy, not-yet-imported meetings.
                    Recommended = !already && !e.IsAllDay
                        && !string.Equals(e.ShowAs, "free", StringComparison.OrdinalIgnoreCase),
                    SuggestedTaskId = tag?.TaskId,
                    SuggestedAttributes = tag?.Attributes ?? [],
                };
            }).ToList();

            return Results.Ok(new CalendarPreviewResult { UsingSampleData = useSample, Meetings = meetings });
        });

        // Import the chosen meetings as time entries (idempotent on the occurrence key).
        api.MapPost("/api/organizations/{orgId:int}/calendar/import", async (
            int orgId, CalendarImportRequest request, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org is null || !await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var validFieldIds = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId && f.IsActive)
                .Select(f => f.Id)
                .ToListAsync();

            var uids = request.Meetings.Select(m => m.SeriesUid).Distinct().ToList();

            // Occurrence keys already imported — so re-running an import never duplicates.
            var existing = await db.TimeEntries
                .Where(e => e.OrganizationId == orgId && e.UserId == userId
                    && e.SourceSeriesUid != null && uids.Contains(e.SourceSeriesUid))
                .Select(e => new { e.SourceSeriesUid, e.SourceOccurrenceStartUtc })
                .ToListAsync();
            var seen = existing
                .Select(e => OccurrenceKey(e.SourceSeriesUid, e.SourceOccurrenceStartUtc))
                .ToHashSet();

            // Series tags we may upsert, loaded once and tracked.
            var tags = await db.CalendarSeriesTags
                .Include(t => t.Attributes)
                .Where(t => t.OrganizationId == orgId && t.UserId == userId && uids.Contains(t.SeriesUid))
                .ToDictionaryAsync(t => t.SeriesUid);

            var imported = 0;
            var skipped = 0;

            foreach (var m in request.Meetings)
            {
                var key = OccurrenceKey(m.SeriesUid, m.OccurrenceStartUtc);
                if (!seen.Add(key))
                {
                    skipped++;
                    continue;
                }

                var entry = new TimeEntry
                {
                    UserId = userId,
                    OrganizationId = orgId,
                    TaskId = m.TaskId,
                    EntryDate = m.EntryDate,
                    StartTime = org.RequireTime ? m.StartTime : null,
                    DurationMinutes = m.DurationMinutes,
                    Note = string.IsNullOrWhiteSpace(m.Subject) ? "(no subject)" : m.Subject.Trim(),
                    Source = TimeEntrySource.OutlookGraph,
                    SourceSeriesUid = m.SeriesUid,
                    SourceEventId = m.EventId,
                    SourceOccurrenceStartUtc = m.OccurrenceStartUtc,
                    CreatedUtc = DateTime.UtcNow,
                };

                foreach (var (fieldId, value) in m.Attributes)
                {
                    if (validFieldIds.Contains(fieldId) && !string.IsNullOrWhiteSpace(value))
                    {
                        entry.Attributes.Add(new TimeEntryAttribute { TimeEntryFieldId = fieldId, Value = value });
                    }
                }

                db.TimeEntries.Add(entry);
                imported++;

                if (m.ApplyToSeries)
                {
                    UpsertSeriesTag(db, tags, orgId, userId, m, validFieldIds);
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new CalendarImportResult(imported, skipped));
        });

        // --- "Connect Outlook" OAuth (Phase 2) ---

        // Connection status for the current user (+ whether the server is configured at all).
        api.MapGet("/api/organizations/{orgId:int}/calendar/connection", async (
            int orgId, TimeTrackerDbContext db, ICurrentUser currentUser,
            ICalendarTokenProvider tokens, IOptions<GraphOptions> graphOptions) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            var info = await tokens.GetConnectionAsync(userId);
            return Results.Ok(new CalendarConnectionStatusDto(
                info is not null, info?.AccountEmail, graphOptions.Value.IsConfigured, info?.LastSyncUtc));
        });

        // Build the sign-in URL; the client navigates the browser to it.
        api.MapPost("/api/organizations/{orgId:int}/calendar/connect-url", async (
            int orgId, HttpContext http, TimeTrackerDbContext db, ICurrentUser currentUser,
            OutlookOAuthClient oauth, IOptions<GraphOptions> graphOptions, IDataProtectionProvider dp) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }
            if (!graphOptions.Value.IsConfigured)
            {
                return NotConfigured();
            }

            var state = StateProtector(dp).Protect($"u:{userId}", TimeSpan.FromMinutes(10));
            var url = oauth.BuildAuthorizeUrl(RedirectUri(graphOptions.Value, http), state);
            return Results.Ok(new ConnectUrlResponse(url));
        });

        // Build the admin-consent URL (an org admin grants the app tenant-wide in one click).
        api.MapGet("/api/organizations/{orgId:int}/calendar/admin-consent-url", async (
            int orgId, HttpContext http, TimeTrackerDbContext db, ICurrentUser currentUser,
            OutlookOAuthClient oauth, IOptions<GraphOptions> graphOptions, IDataProtectionProvider dp) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }
            if (!graphOptions.Value.IsConfigured)
            {
                return NotConfigured();
            }

            var state = StateProtector(dp).Protect($"a:{userId}", TimeSpan.FromMinutes(30));
            var url = oauth.BuildAdminConsentUrl(RedirectUri(graphOptions.Value, http), state);
            return Results.Ok(new ConnectUrlResponse(url));
        });

        // Disconnect.
        api.MapDelete("/api/organizations/{orgId:int}/calendar/connection", async (
            int orgId, TimeTrackerDbContext db, ICurrentUser currentUser, ICalendarTokenProvider tokens) =>
        {
            var userId = await currentUser.GetUserIdAsync();
            if (!await IsMemberAsync(db, userId, orgId))
            {
                return Results.Forbid();
            }

            await tokens.DeleteConnectionAsync(userId);
            return Results.NoContent();
        });

        // OAuth redirect target — hit by the browser (no bearer); identity comes from the
        // protected state, so this is intentionally anonymous.
        app.MapGet("/api/calendar/callback", async (
            HttpContext http, OutlookOAuthClient oauth, ICalendarTokenProvider tokens,
            IOptions<GraphOptions> graphOptions, IDataProtectionProvider dp, IConfiguration config) =>
        {
            var webBase = (config["WebApp:BaseUrl"] ?? "http://localhost:5008").TrimEnd('/');
            var q = http.Request.Query;

            // Admin-consent return carries admin_consent rather than an auth code.
            if (q.ContainsKey("admin_consent"))
            {
                var ok = string.Equals(q["admin_consent"], "True", StringComparison.OrdinalIgnoreCase);
                return Results.Redirect($"{webBase}/import?adminConsent={(ok ? "ok" : "error")}");
            }

            if (q.ContainsKey("error"))
            {
                return Results.Redirect($"{webBase}/import?connected=error");
            }

            int userId;
            try
            {
                var payload = StateProtector(dp).Unprotect(q["state"].ToString());
                if (!payload.StartsWith("u:") || !int.TryParse(payload[2..], out userId))
                {
                    return Results.Redirect($"{webBase}/import?connected=error");
                }
            }
            catch
            {
                return Results.Redirect($"{webBase}/import?connected=error");
            }

            try
            {
                var t = await oauth.RedeemCodeAsync(q["code"].ToString(), RedirectUri(graphOptions.Value, http));
                await tokens.SaveConnectionAsync(userId, t);
                return Results.Redirect($"{webBase}/import?connected=ok");
            }
            catch (CalendarSourceException)
            {
                return Results.Redirect($"{webBase}/import?connected=error");
            }
        }).AllowAnonymous();
    }

    private static ITimeLimitedDataProtector StateProtector(IDataProtectionProvider dp) =>
        dp.CreateProtector("TimeTracker.Calendar.OAuthState").ToTimeLimitedDataProtector();

    private static string RedirectUri(GraphOptions o, HttpContext http) =>
        !string.IsNullOrWhiteSpace(o.RedirectUri)
            ? o.RedirectUri
            : $"{http.Request.Scheme}://{http.Request.Host}/api/calendar/callback";

    private static IResult NotConfigured() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["graph"] = ["Outlook isn't set up on the server yet (no Entra app registration). Add Graph:ClientId/ClientSecret."],
    });

    // Remember (or update) the task/field values for a meeting series so future occurrences
    // can be auto-tagged.
    private static void UpsertSeriesTag(
        TimeTrackerDbContext db, Dictionary<string, CalendarSeriesTag> tags,
        int orgId, int userId, ImportMeetingRequest m, IReadOnlyList<int> validFieldIds)
    {
        if (!tags.TryGetValue(m.SeriesUid, out var tag))
        {
            tag = new CalendarSeriesTag
            {
                OrganizationId = orgId,
                UserId = userId,
                SeriesUid = m.SeriesUid,
                CreatedUtc = DateTime.UtcNow,
            };
            db.CalendarSeriesTags.Add(tag);
            tags[m.SeriesUid] = tag;
        }
        else
        {
            tag.ModifiedUtc = DateTime.UtcNow;
            db.CalendarSeriesTagAttributes.RemoveRange(tag.Attributes);
            tag.Attributes.Clear();
        }

        tag.TaskId = m.TaskId;
        foreach (var (fieldId, value) in m.Attributes)
        {
            if (validFieldIds.Contains(fieldId) && !string.IsNullOrWhiteSpace(value))
            {
                tag.Attributes.Add(new CalendarSeriesTagAttribute { TimeEntryFieldId = fieldId, Value = value });
            }
        }
    }

    // Stable de-dup key for an occurrence. Formats without a zone designator so a value read
    // back from datetime2 (Kind=Unspecified) matches the same instant arriving as Kind=Utc.
    private static string OccurrenceKey(string? seriesUid, DateTime? occurrenceStartUtc) =>
        $"{seriesUid}|{occurrenceStartUtc?.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)}";

    private static (DateTimeOffset From, DateTimeOffset To) ToUtcWindow(DateOnly from, DateOnly to)
    {
        var fromLocal = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
        var toLocal = DateTime.SpecifyKind(to.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Local);
        return (new DateTimeOffset(fromLocal).ToUniversalTime(), new DateTimeOffset(toLocal).ToUniversalTime());
    }

    private static Task<bool> IsMemberAsync(TimeTrackerDbContext db, int userId, int orgId) =>
        db.UserOrganizations.AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
}
