namespace TimeTracker.Domain.Entities;

/// <summary>
/// A task a user may need to work on. Optional target of a time entry — logging
/// activity does not require a task. Table: <c>t_task</c>.
/// </summary>
public class TaskItem : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Optional org the task belongs to (null = personal/unassigned).</summary>
    public int? OrganizationId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }

    /// <summary>Optional estimate of effort in hours.</summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>Completion percentage, 0–100.</summary>
    public int PercentComplete { get; set; }

    public User User { get; set; } = null!;
    public Organization? Organization { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
