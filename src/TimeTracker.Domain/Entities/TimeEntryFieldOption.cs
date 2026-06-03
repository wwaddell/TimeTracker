namespace TimeTracker.Domain.Entities;

/// <summary>
/// An allowed value for a select/dropdown-type configurable field.
/// Table: <c>t_time_entry_field_option</c>.
/// </summary>
public class TimeEntryFieldOption
{
    public int Id { get; set; }
    public int TimeEntryFieldId { get; set; }

    /// <summary>Stored value (what lands in the attribute row).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Display label for the option.</summary>
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TimeEntryField TimeEntryField { get; set; } = null!;
}
