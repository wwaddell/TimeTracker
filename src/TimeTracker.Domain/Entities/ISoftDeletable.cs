namespace TimeTracker.Domain.Entities;

/// <summary>
/// Marks an entity that is soft-deleted rather than removed: <see cref="DeletedUtc"/> is set
/// instead of deleting the row. A global EF query filter excludes these from all reads, so the
/// data is preserved (recoverable/auditable) while disappearing from the app.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedUtc { get; set; }
}
