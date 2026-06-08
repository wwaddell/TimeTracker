using TimeTracker.Domain.Enums;

namespace TimeTracker.Contracts.Tasks;

/// <summary>A task the user may work on (and optionally log time against).</summary>
public record TaskDto(
    int Id,
    string Title,
    string? Description,
    bool IsComplete,
    decimal? EstimatedHours,
    int PercentComplete,
    int? PercentBeforeComplete,
    TaskPriority? Priority,
    DateOnly? DueDate,
    int? ProjectId,
    string? ProjectName,
    string? ReferenceCode,
    string? ExternalUrl,
    int AssignedToUserId,
    string AssignedToName,
    DateTime CreatedUtc);

/// <summary>Create/update payload for a task.</summary>
public record SaveTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsComplete { get; init; }
    public decimal? EstimatedHours { get; init; }
    public int PercentComplete { get; init; }

    /// <summary>Percent to restore to if completion is later removed (only kept while complete).</summary>
    public int? PercentBeforeComplete { get; init; }

    /// <summary>Optional priority (null = none).</summary>
    public TaskPriority? Priority { get; init; }

    /// <summary>Optional due date.</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Optional first-class project this task is linked to.</summary>
    public int? ProjectId { get; init; }

    /// <summary>Optional short tag (e.g. external ticket id).</summary>
    public string? ReferenceCode { get; init; }

    /// <summary>Optional external URL (e.g. the task's page in another system).</summary>
    public string? ExternalUrl { get; init; }

    /// <summary>
    /// Assignee user id. 0 means "default to the current user" (new task) or "leave unchanged"
    /// (update). Otherwise the user must be a member of the same organization.
    /// </summary>
    public int AssignedToUserId { get; init; }
}

/// <summary>One change event in a task's audit history (newest-first when listed).</summary>
/// <param name="ChangeType">"Created" | "Updated" | "Deleted" | "Restored".</param>
/// <param name="FieldName">CLR property name on TaskItem for Updated rows; null for markers.</param>
/// <param name="OldValue">Stringified previous value; null for markers or null-to-X transitions.</param>
/// <param name="NewValue">Stringified new value; null for markers or X-to-null transitions.</param>
public record TaskHistoryDto(
    long Id,
    DateTime ChangedUtc,
    int? ChangedByUserId,
    string? ChangedByName,
    string ChangeType,
    string? FieldName,
    string? OldValue,
    string? NewValue);
