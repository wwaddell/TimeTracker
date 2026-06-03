namespace TimeTracker.Domain.Entities;

/// <summary>
/// Base class for entities that carry creation/modification audit timestamps (UTC).
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
