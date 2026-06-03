namespace TimeTracker.Domain.Entities;

/// <summary>
/// A role defined by a single organization. Roles are org-specific and independent
/// across organizations. A user's role drives which time-entry fields they see.
/// Table: <c>t_organization_role</c>.
/// </summary>
public class OrganizationRole : AuditableEntity
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<UserOrganization> Members { get; set; } = new List<UserOrganization>();
}
