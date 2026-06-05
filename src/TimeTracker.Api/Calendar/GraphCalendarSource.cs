using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TimeTracker.Api.Calendar;

/// <summary>
/// Reads Outlook/Microsoft 365 calendars via the Microsoft Graph <c>/me/calendarView</c> endpoint,
/// which expands recurring series into individual occurrences within the requested window.
/// The access token is supplied per call (from a stored connection in production, or pasted in dev).
/// </summary>
public sealed class GraphCalendarSource(HttpClient http) : ICalendarSource
{
    // Select only the fields we map; calendarView expands recurrences for us.
    private const string Select =
        "id,iCalUId,seriesMasterId,type,subject,start,end,isAllDay,showAs,organizer";

    // Safety cap so a huge calendar window can't pull an unbounded result set into memory.
    private const int MaxEvents = 1000;

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        string accessToken, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default)
    {
        var results = new List<CalendarEvent>();

        var start = fromUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var end = toUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var url = $"me/calendarView?startDateTime={start}&endDateTime={end}"
                + $"&$select={Select}&$orderby=start/dateTime&$top=100";

        // Follow @odata.nextLink until the window is fully paged (or the safety cap is hit).
        while (url is not null && results.Count < MaxEvents)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // Ask Graph to express all start/end times in UTC so we don't have to interpret zones.
            request.Headers.Add("Prefer", "outlook.timezone=\"UTC\"");

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new CalendarSourceException(DescribeFailure(response.StatusCode));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            if (root.TryGetProperty("value", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var mapped = Map(item);
                    if (mapped is not null)
                    {
                        results.Add(mapped);
                    }
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }

        return results;
    }

    private static CalendarEvent? Map(JsonElement e)
    {
        var startUtc = ReadGraphDateTime(e, "start");
        var endUtc = ReadGraphDateTime(e, "end");
        if (startUtc is null || endUtc is null)
        {
            return null;
        }

        var id = GetString(e, "id") ?? string.Empty;
        // Fall back to the event id if a series UID is somehow absent, so dedup still has a key.
        var seriesUid = GetString(e, "iCalUId") ?? id;
        var type = GetString(e, "type"); // singleInstance | occurrence | exception | seriesMaster
        var isRecurring = type is not null && !type.Equals("singleInstance", StringComparison.OrdinalIgnoreCase);

        string? organizer = null;
        if (e.TryGetProperty("organizer", out var org)
            && org.TryGetProperty("emailAddress", out var addr))
        {
            organizer = GetString(addr, "name") ?? GetString(addr, "address");
        }

        return new CalendarEvent(
            SeriesUid: seriesUid,
            EventId: id,
            Subject: GetString(e, "subject") ?? "(no subject)",
            StartUtc: startUtc.Value,
            EndUtc: endUtc.Value,
            IsAllDay: e.TryGetProperty("isAllDay", out var allDay) && allDay.ValueKind == JsonValueKind.True,
            ShowAs: GetString(e, "showAs"),
            IsRecurring: isRecurring,
            OrganizerName: organizer);
    }

    // Graph dateTimeTimeZone: { "dateTime": "2026-06-03T09:00:00.0000000", "timeZone": "UTC" }.
    private static DateTimeOffset? ReadGraphDateTime(JsonElement parent, string prop)
    {
        if (!parent.TryGetProperty(prop, out var dt) || !dt.TryGetProperty("dateTime", out var v))
        {
            return null;
        }

        var raw = v.GetString();
        if (string.IsNullOrEmpty(raw)
            || !DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
    }

    private static string? GetString(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string DescribeFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized =>
            "Outlook rejected the access token (it may be expired or for the wrong resource). Reconnect or paste a fresh token.",
        HttpStatusCode.Forbidden =>
            "Outlook denied access (403). Most often the token is missing the Calendars.Read scope — in Graph Explorer, "
            + "consent to Calendars.Read, run a calendar query, then copy a fresh token (check it at jwt.ms: the 'scp' claim "
            + "should list Calendars.Read). If you can't grant that scope, your organization requires admin consent for it.",
        HttpStatusCode.TooManyRequests =>
            "Microsoft Graph is throttling requests. Wait a moment and try again.",
        _ => $"Microsoft Graph returned an error ({(int)status} {status}).",
    };
}
