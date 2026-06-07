using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A logged time entry. Core fields are always present; org/role-defined extra
/// fields are stored as <see cref="Attributes"/> rows. Table: <c>t_time_entry</c>.
/// </summary>
public class TimeEntry : AuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Optional task this entry is logged against.</summary>
    public int? TaskId { get; set; }

    /// <summary>Optional first-class project this entry is logged against.</summary>
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>How this entry was created (manual vs. an external import).</summary>
    public TimeEntrySource Source { get; set; } = TimeEntrySource.Manual;

    /// <summary>
    /// For imported entries, the calendar series identity (Graph <c>iCalUId</c>).
    /// The same for every occurrence of a recurring meeting — the key for series tagging.
    /// </summary>
    public string? SourceSeriesUid { get; set; }

    /// <summary>For imported entries, the source occurrence id (Graph event <c>id</c>).</summary>
    public string? SourceEventId { get; set; }

    /// <summary>For imported entries, whether the meeting was part of a recurring series.</summary>
    public bool SourceIsRecurring { get; set; }

    /// <summary>
    /// For imported entries, the specific occurrence's start instant (UTC). Combined with
    /// <see cref="SourceSeriesUid"/> this uniquely identifies an occurrence for de-duplication.
    /// </summary>
    public DateTime? SourceOccurrenceStartUtc { get; set; }

    /// <summary>Date the activity occurred (defaults to today, editable).</summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>Optional start time of the activity (defaults to now, editable).</summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Optional duration of the activity in minutes.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>The always-required base field: a note of what was completed.</summary>
    public string Note { get; set; } = string.Empty;

    // --- Auto-captured context (opt-in via User.CaptureLocation). Hidden from the UI;
    // intended for audit, dispute-resolution, and future "where did I do this" reports. ---

    /// <summary>Latitude in WGS-84 decimal degrees if the browser provided it.</summary>
    public double? Latitude { get; set; }
    /// <summary>Longitude in WGS-84 decimal degrees if the browser provided it.</summary>
    public double? Longitude { get; set; }
    /// <summary>Geolocation accuracy in meters as reported by the browser.</summary>
    public double? LocationAccuracy { get; set; }
    /// <summary>IANA timezone name (e.g. <c>America/New_York</c>) of the client at create time.</summary>
    public string? Timezone { get; set; }

    /// <summary>Set when soft-deleted; excluded from queries by a global filter.</summary>
    public DateTime? DeletedUtc { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public TaskItem? Task { get; set; }
    public ICollection<TimeEntryAttribute> Attributes { get; set; } = new List<TimeEntryAttribute>();
}
