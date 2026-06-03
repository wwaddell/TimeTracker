namespace TimeTracker.Domain.Entities;

/// <summary>
/// A key-value (EAV) attribute holding the value of one configurable field for a
/// time entry. The "key" is a FK to the field definition (not a free string), which
/// keeps values clean and reportable. Table: <c>t_time_entry_attribute</c>.
/// </summary>
public class TimeEntryAttribute
{
    public long Id { get; set; }
    public long TimeEntryId { get; set; }
    public int TimeEntryFieldId { get; set; }

    /// <summary>Raw value, interpreted according to the field's data type. Null = not provided.</summary>
    public string? Value { get; set; }

    public TimeEntry TimeEntry { get; set; } = null!;
    public TimeEntryField TimeEntryField { get; set; } = null!;
}
