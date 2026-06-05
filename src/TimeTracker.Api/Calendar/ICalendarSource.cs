namespace TimeTracker.Api.Calendar;

/// <summary>
/// Reads calendar occurrences for the signed-in user from an external source.
/// The import pipeline (preview/import endpoints) is written against this abstraction so the
/// concrete source — Microsoft Graph today, others later — can change without touching it.
/// </summary>
public interface ICalendarSource
{
    /// <summary>
    /// Fetches occurrences between <paramref name="fromUtc"/> and <paramref name="toUtc"/> using the
    /// supplied OAuth access token. Recurring meetings are expanded into individual occurrences.
    /// </summary>
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        string accessToken, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default);
}

/// <summary>Raised when the calendar source rejects the request (e.g. an expired token).</summary>
public sealed class CalendarSourceException(string message) : Exception(message);
