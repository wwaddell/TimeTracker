namespace TimeTracker.Domain.Entities;

/// <summary>
/// A project an organization tracks work against. Time entries and tasks can be linked to a
/// project. Visibility is org-wide by default; when <see cref="IsRestricted"/> is true, only
/// the users listed in <see cref="Members"/> can see/pick the project.
/// Soft-deleted (a global query filter on <see cref="DeletedUtc"/> hides removed projects).
/// Table: <c>t_project</c>.
/// </summary>
public class Project : AuditableEntity, ISoftDeletable
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Inactive projects are hidden from new entries but kept for history.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>False = visible to all org members; true = only the listed <see cref="Members"/>.</summary>
    public bool IsRestricted { get; set; }

    public DateTime? DeletedUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
