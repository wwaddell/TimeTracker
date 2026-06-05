namespace TimeTracker.Api.Calendar;

/// <summary>
/// Synthetic calendar occurrences for development, so the whole import flow (fetch → review →
/// dedup → recurring series auto-tag) can be exercised without an Entra app registration or a
/// real Graph token. Used only in Development when the access token is blank or the sentinel
/// <see cref="Sentinel"/>. Includes a recurring daily series, a weekly series, single meetings,
/// and an all-day event (to verify filtering).
/// </summary>
public static class DevCalendarSamples
{
    /// <summary>Paste this as the token (in dev) to use sample meetings instead of real Graph.</summary>
    public const string Sentinel = "FAKE";

    public static IReadOnlyList<CalendarEvent> Build(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        var events = new List<CalendarEvent>();
        var fromDate = fromUtc.ToLocalTime().Date;
        var toDate = toUtc.ToLocalTime().Date;

        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            var weekday = day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            if (weekday)
            {
                // Daily recurring stand-up — every occurrence shares the same series UID.
                events.Add(Local(
                    seriesUid: "series-standup@timetracker.dev",
                    eventId: $"standup-{day:yyyyMMdd}",
                    subject: "Daily Team Stand-up",
                    start: day.AddHours(9), durationMinutes: 15,
                    isRecurring: true, organizer: "Scrum Master"));
            }

            if (day.DayOfWeek == DayOfWeek.Monday)
            {
                // Weekly recurring planning.
                events.Add(Local(
                    seriesUid: "series-planning@timetracker.dev",
                    eventId: $"planning-{day:yyyyMMdd}",
                    subject: "Sprint Planning",
                    start: day.AddHours(10), durationMinutes: 60,
                    isRecurring: true, organizer: "Product Owner"));
            }
        }

        // A couple of one-off meetings.
        events.Add(Local(
            seriesUid: "single-1on1@timetracker.dev",
            eventId: "single-1on1",
            subject: "1:1 with Manager",
            start: fromDate.AddDays(1).AddHours(14), durationMinutes: 30,
            isRecurring: false, organizer: "Manager"));

        events.Add(Local(
            seriesUid: "single-client@timetracker.dev",
            eventId: "single-client",
            subject: "Client Onboarding Call",
            start: fromDate.AddDays(2).AddHours(11).AddMinutes(30), durationMinutes: 45,
            isRecurring: false, organizer: "Account Exec"));

        // An all-day event — the importer should filter this out by default.
        var holiday = fromDate.AddDays(3);
        events.Add(new CalendarEvent(
            SeriesUid: "single-holiday@timetracker.dev",
            EventId: "single-holiday",
            Subject: "Company Holiday",
            StartUtc: new DateTimeOffset(holiday, TimeZoneInfo.Local.GetUtcOffset(holiday)).ToUniversalTime(),
            EndUtc: new DateTimeOffset(holiday.AddDays(1), TimeZoneInfo.Local.GetUtcOffset(holiday.AddDays(1))).ToUniversalTime(),
            IsAllDay: true,
            ShowAs: "free",
            IsRecurring: false,
            OrganizerName: "HR"));

        return events
            .Where(e => e.StartUtc >= fromUtc && e.StartUtc <= toUtc)
            .OrderBy(e => e.StartUtc)
            .ToList();
    }

    private static CalendarEvent Local(
        string seriesUid, string eventId, string subject,
        DateTime start, int durationMinutes, bool isRecurring, string organizer)
    {
        var startLocal = DateTime.SpecifyKind(start, DateTimeKind.Local);
        var endLocal = startLocal.AddMinutes(durationMinutes);
        return new CalendarEvent(
            SeriesUid: seriesUid,
            EventId: eventId,
            Subject: subject,
            StartUtc: new DateTimeOffset(startLocal).ToUniversalTime(),
            EndUtc: new DateTimeOffset(endLocal).ToUniversalTime(),
            IsAllDay: false,
            ShowAs: "busy",
            IsRecurring: isRecurring,
            OrganizerName: organizer);
    }
}
