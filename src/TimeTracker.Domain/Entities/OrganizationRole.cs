namespace TimeTracker.Domain.Entities;

/// <summary>
/// A role defined by a single organization. Roles are org-specific and independent
/// across organizations. A role grants a set of <see cref="Rights"/> and can be held
/// by multiple members. Table: <c>t_organization_role</c>.
/// </summary>
public class OrganizationRole : AuditableEntity
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Organization Organization { get; set; } = null!;

    /// <summary>Rights granted to this role.</summary>
    public ICollection<OrganizationRoleRight> Rights { get; set; } = new List<OrganizationRoleRight>();

    /// <summary>Membership-role links (the users holding this role).</summary>
    public ICollection<UserOrganizationRole> Members { get; set; } = new List<UserOrganizationRole>();
}
