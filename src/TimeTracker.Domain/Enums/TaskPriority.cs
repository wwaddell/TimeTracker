namespace TimeTracker.Domain.Enums;

/// <summary>Optional priority of a task. Stored as an int; null = no priority set.</summary>
public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4,
}
