using TimeTracker.Domain.Enums;

namespace TimeTracker.Contracts.TimeEntries;

/// <summary>An organization the current user can log time for.</summary>
public record OrganizationDto(int Id, string Name, string? RoleName, bool RequireTime = true);

/// <summary>An allowed value for a select-type configurable field.</summary>
public record EntryFieldOptionDto(string Value, string Label, string? Icon = null);

/// <summary>Definition of a configurable extra field shown on the entry form.</summary>
public record EntryFieldDto(
    int Id,
    string FieldKey,
    string Label,
    FieldDataType DataType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<EntryFieldOptionDto> Options);

/// <summary>Payload to create a time entry. Note is the only always-required field.</summary>
public record CreateTimeEntryRequest
{
    public string Note { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public int? DurationMinutes { get; init; }
    public int? TaskId { get; init; }

    /// <summary>Optional first-class project this entry is logged against.</summary>
    public int? ProjectId { get; init; }

    // --- Auto-captured context. Hidden from the UI. Sent by the client when the user
    // has opted into location capture; the timezone is always sent. Server stores verbatim. ---

    /// <summary>Latitude (WGS-84 decimal degrees) if the browser granted location.</summary>
    public double? Latitude { get; init; }
    /// <summary>Longitude (WGS-84 decimal degrees) if the browser granted location.</summary>
    public double? Longitude { get; init; }
    /// <summary>Geolocation accuracy in meters as reported by the browser.</summary>
    public double? LocationAccuracy { get; init; }
    /// <summary>IANA timezone name of the client at create time (e.g. <c>America/New_York</c>).</summary>
    public string? Timezone { get; init; }

    /// <summary>Values for configurable fields, keyed by TimeEntryField Id.</summary>
    public Dictionary<int, string?> Attributes { get; init; } = new();
}

/// <summary>A stored attribute value, with its field label for display.</summary>
public record TimeEntryAttributeDto(int FieldId, string Label, string? Value);

/// <summary>A logged time entry returned to clients.</summary>
/// <param name="GroupKey">When the list is grouped (?group=day|week|month), the key that puts this
/// entry under one of <see cref="TimeEntriesPage.Groups"/>. Stable string like "2026-06-07",
/// "2026-W23", or "2026-06". Empty when not grouping.</param>
public record TimeEntryDto(
    long Id,
    DateOnly EntryDate,
    TimeOnly? StartTime,
    int? DurationMinutes,
    string Note,
    int? TaskId,
    string? TaskTitle,
    int? ProjectId,
    string? ProjectName,
    DateTime CreatedUtc,
    TimeEntrySource Source,
    bool SourceIsRecurring,
    string GroupKey,
    IReadOnlyList<TimeEntryAttributeDto> Attributes);

/// <summary>
/// One group bucket on a grouped Log Time list. Stats are for the FULL group (not just the
/// page slice) so the header reads consistently even when a group spans pages.
/// </summary>
public record TimeEntryGroupDto(string Key, string Label, int TotalMinutes, int EntryCount, int IssueCount);

/// <summary>
/// Paged time-entry list with optional grouping. When <c>group</c> wasn't asked for,
/// <see cref="Groups"/> is empty and each entry's GroupKey is "".
/// </summary>
public record TimeEntriesPage(
    IReadOnlyList<TimeEntryDto> Items,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<TimeEntryGroupDto> Groups);
