namespace TimeTracker.Web.Models;

/// <summary>Editable model backing both the create form and the edit dialog.</summary>
public class TimeEntryModel
{
    public string Note { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public TimeOnly? StartTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
    public int? DurationMinutes { get; set; }
    public int? TaskId { get; set; }
    public int? ProjectId { get; set; }
    public Dictionary<int, string?> Attributes { get; set; } = new();
}
