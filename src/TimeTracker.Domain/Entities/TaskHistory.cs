using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// One change event on a <see cref="TaskItem"/>. Rows are appended by the DbContext's
/// SaveChanges override whenever a TaskItem is added, modified, or soft-deleted —
/// so it catches every code path that mutates a task, not just the PUT endpoint.
/// Table: <c>t_task_history</c>.
/// </summary>
/// <remarks>
/// Row shapes by <see cref="ChangeType"/>:
/// - Created/Deleted/Restored: marker row, FieldName/OldValue/NewValue are all null.
/// - Updated: one row per changed scalar field, FieldName is the CLR property name,
///   OldValue/NewValue are string-formatted (or null).
/// </remarks>
public class TaskHistory
{
    public long Id { get; set; }

    /// <summary>The task this change happened to. Restrict-delete so history outlives soft-deletes.</summary>
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    /// <summary>
    /// User who triggered the change. Nullable so design-time / dev-seeding mutations
    /// (no HTTP context) still produce history rows.
    /// </summary>
    public int? ChangedByUserId { get; set; }
    public User? ChangedBy { get; set; }

    public DateTime ChangedUtc { get; set; }

    public TaskChangeType ChangeType { get; set; }

    /// <summary>Property name on TaskItem, e.g. "Title", "Priority". Null for Created/Deleted/Restored markers.</summary>
    public string? FieldName { get; set; }

    /// <summary>Stringified previous value (null when transitioning from null OR for markers).</summary>
    public string? OldValue { get; set; }

    /// <summary>Stringified new value (null when transitioning to null OR for markers).</summary>
    public string? NewValue { get; set; }
}
