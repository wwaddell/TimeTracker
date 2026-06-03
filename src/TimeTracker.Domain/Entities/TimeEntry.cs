namespace TimeTracker.Domain.Entities;

/// <summary>
/// A logged time entry. Core fields are always present; org/role-defined extra
/// fields are stored as <see cref="Attributes"/> rows. Table: <c>t_time_entry</c>.
/// </summary>
public class TimeEntry : AuditableEntity
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Optional task this entry is logged against.</summary>
    public int? TaskId { get; set; }

    /// <summary>Date the activity occurred (defaults to today, editable).</summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>Optional start time of the activity (defaults to now, editable).</summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Optional duration of the activity in minutes.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>The always-required base field: a note of what was completed.</summary>
    public string Note { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public TaskItem? Task { get; set; }
    public ICollection<TimeEntryAttribute> Attributes { get; set; } = new List<TimeEntryAttribute>();
}
