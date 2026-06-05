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
    string? Project,
    int? ProjectId,
    string? ProjectName,
    string? ReferenceCode,
    string? ExternalUrl,
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

    /// <summary>Optional legacy project (configurable "Project" field value).</summary>
    public string? Project { get; init; }

    /// <summary>Optional first-class project this task is linked to (preferred).</summary>
    public int? ProjectId { get; init; }

    /// <summary>Optional short tag (e.g. external ticket id).</summary>
    public string? ReferenceCode { get; init; }

    /// <summary>Optional external URL (e.g. the task's page in another system).</summary>
    public string? ExternalUrl { get; init; }
}
