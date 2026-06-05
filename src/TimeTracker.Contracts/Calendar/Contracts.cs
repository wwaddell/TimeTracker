namespace TimeTracker.Contracts.Calendar;

/// <summary>Request to preview calendar meetings to import for a date range.</summary>
public record CalendarPreviewRequest
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    /// <summary>
    /// OAuth access token for Microsoft Graph. In dev, leave blank or pass "FAKE" to use
    /// sample meetings. In production this comes from the user's stored Outlook connection.
    /// </summary>
    public string? AccessToken { get; init; }
}

/// <summary>A field value remembered/suggested for a meeting (keyed by TimeEntryField id).</summary>
public record CalendarAttributeValue(int FieldId, string? Value);

/// <summary>A single calendar occurrence offered for import.</summary>
public record CalendarMeetingDto
{
    public string SeriesUid { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public DateTime OccurrenceStartUtc { get; init; }

    public string Subject { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public int DurationMinutes { get; init; }

    public bool IsRecurring { get; init; }
    public bool IsAllDay { get; init; }
    public string? ShowAs { get; init; }
    public string? OrganizerName { get; init; }

    /// <summary>Already logged (matched an existing imported entry) — shown but not importable.</summary>
    public bool AlreadyImported { get; init; }

    /// <summary>Default-selected for import (a real, not-yet-imported meeting).</summary>
    public bool Recommended { get; init; }

    /// <summary>Task remembered for this meeting's series, if previously tagged.</summary>
    public int? SuggestedTaskId { get; init; }

    /// <summary>
    /// Project suggested by the server (auto-mapped from the subject via a project's ReferenceCode).
    /// </summary>
    public int? SuggestedProjectId { get; init; }

    /// <summary>Field values remembered for this meeting's series, if previously tagged.</summary>
    public IReadOnlyList<CalendarAttributeValue> SuggestedAttributes { get; init; } = [];
}

/// <summary>Result of a preview fetch.</summary>
public record CalendarPreviewResult
{
    /// <summary>True when sample (dev) data was returned instead of a live Graph call.</summary>
    public bool UsingSampleData { get; init; }
    public IReadOnlyList<CalendarMeetingDto> Meetings { get; init; } = [];
}

/// <summary>One meeting the user chose to import, with its mapping and tagging choices.</summary>
public record ImportMeetingRequest
{
    public string SeriesUid { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public DateTime OccurrenceStartUtc { get; init; }

    public string Subject { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public int? DurationMinutes { get; init; }
    public bool IsRecurring { get; init; }

    public int? TaskId { get; init; }

    /// <summary>The chosen project (either the server suggestion or the user's pick).</summary>
    public int? ProjectId { get; init; }

    public Dictionary<int, string?> Attributes { get; init; } = new();
}

/// <summary>Batch import request.</summary>
public record CalendarImportRequest
{
    public IReadOnlyList<ImportMeetingRequest> Meetings { get; init; } = [];
}

/// <summary>Outcome of an import: how many entries were created vs. skipped as duplicates.</summary>
public record CalendarImportResult(int Imported, int Skipped);

/// <summary>Whether the user has linked Outlook, and whether the server is even configured for it.</summary>
public record CalendarConnectionStatusDto(bool Connected, string? AccountEmail, bool Configured, DateTime? LastSyncUtc);

/// <summary>A URL to send the browser to (OAuth sign-in or admin consent).</summary>
public record ConnectUrlResponse(string Url);
