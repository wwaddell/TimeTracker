using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A task a user may need to work on. Optional target of a time entry — logging
/// activity does not require a task. Table: <c>t_task</c>.
/// </summary>
public class TaskItem : AuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>The user who created the task. Stays fixed; ownership is via <see cref="AssignedToUserId"/>.</summary>
    public int UserId { get; set; }

    /// <summary>
    /// The user the task is assigned to. New tasks default to the creator; reassigning moves the
    /// task off the original assignee's list onto the new assignee's.
    /// </summary>
    public int AssignedToUserId { get; set; }
    public User AssignedTo { get; set; } = null!;

    /// <summary>Optional org the task belongs to (null = personal/unassigned).</summary>
    public int? OrganizationId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }

    /// <summary>Optional first-class project this task belongs to.</summary>
    public int? ProjectId { get; set; }
    public Project? ProjectEntity { get; set; }

    /// <summary>Optional short tag (e.g. an external ticket id like "JIRA-123").</summary>
    public string? ReferenceCode { get; set; }

    /// <summary>Optional external URL (e.g. the task's page in another system).</summary>
    public string? ExternalUrl { get; set; }

    /// <summary>Optional priority (null = none).</summary>
    public TaskPriority? Priority { get; set; }

    /// <summary>Optional due date.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Optional estimate of effort in hours.</summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>Completion percentage, 0–100.</summary>
    public int PercentComplete { get; set; }

    /// <summary>The percent held just before the task was marked complete, so unchecking can revert.</summary>
    public int? PercentBeforeComplete { get; set; }

    /// <summary>Set when soft-deleted; excluded from queries by a global filter.</summary>
    public DateTime? DeletedUtc { get; set; }

    public User User { get; set; } = null!;
    public Organization? Organization { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
