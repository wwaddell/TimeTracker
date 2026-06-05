namespace TimeTracker.Api.Calendar;

/// <summary>
/// A single calendar occurrence pulled from a source (Graph or a dev sample), normalized
/// for the import pipeline. For a recurring meeting, every occurrence shares
/// <see cref="SeriesUid"/> while <see cref="EventId"/> / <see cref="StartUtc"/> are per-occurrence.
/// </summary>
public sealed record CalendarEvent(
    string SeriesUid,
    string EventId,
    string Subject,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsAllDay,
    string? ShowAs,
    bool IsRecurring,
    string? OrganizerName);
