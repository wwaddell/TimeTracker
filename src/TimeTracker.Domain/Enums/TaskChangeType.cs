namespace TimeTracker.Domain.Enums;

/// <summary>
/// What kind of change a <c>t_task_history</c> row represents. Drives how the
/// timeline renders each row (icon + message).
/// </summary>
public enum TaskChangeType : byte
{
    /// <summary>The task was created. One row per create event; field_name/old/new are null.</summary>
    Created = 1,

    /// <summary>A single scalar field changed. One row per changed field per save.</summary>
    Updated = 2,

    /// <summary>The task was soft-deleted (DeletedUtc went null → value). One row per delete.</summary>
    Deleted = 3,

    /// <summary>A previously-deleted task was restored (DeletedUtc went value → null). One row per restore.</summary>
    Restored = 4,
}
