namespace TimeTracker.Domain.Entities;

/// <summary>
/// Remembers how a user tagged a calendar meeting series (by Graph <c>iCalUId</c>) so the
/// same task/field values can be re-applied automatically to other occurrences — both the
/// rest of the current import and future imports of the same recurring meeting.
/// Table: <c>t_calendar_series_tag</c>.
/// </summary>
public class CalendarSeriesTag : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>The meeting series identity (Graph <c>iCalUId</c>).</summary>
    public string SeriesUid { get; set; } = string.Empty;

    /// <summary>Task to tag occurrences of this series with (optional).</summary>
    public int? TaskId { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public TaskItem? Task { get; set; }

    /// <summary>Configurable field values to apply to occurrences of this series.</summary>
    public ICollection<CalendarSeriesTagAttribute> Attributes { get; set; } = new List<CalendarSeriesTagAttribute>();
}
