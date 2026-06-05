namespace TimeTracker.Contracts.Tasks;

/// <summary>A task the user may work on (and optionally log time against).</summary>
public record TaskDto(
    int Id,
    string Title,
    string? Description,
    bool IsComplete,
    decimal? EstimatedHours,
    int PercentComplete,
    DateTime CreatedUtc);

/// <summary>Create/update payload for a task.</summary>
public record SaveTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsComplete { get; init; }
    public decimal? EstimatedHours { get; init; }
    public int PercentComplete { get; init; }
}
