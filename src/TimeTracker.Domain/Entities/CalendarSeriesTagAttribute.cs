namespace TimeTracker.Domain.Entities;

/// <summary>
/// A configurable field value remembered for a calendar series, mirroring
/// <see cref="TimeEntryAttribute"/>. Table: <c>t_calendar_series_tag_attribute</c>.
/// </summary>
public class CalendarSeriesTagAttribute
{
    public int Id { get; set; }
    public int CalendarSeriesTagId { get; set; }
    public int TimeEntryFieldId { get; set; }

    /// <summary>Raw value, interpreted per the field's data type.</summary>
    public string? Value { get; set; }

    public CalendarSeriesTag CalendarSeriesTag { get; set; } = null!;
    public TimeEntryField TimeEntryField { get; set; } = null!;
}
