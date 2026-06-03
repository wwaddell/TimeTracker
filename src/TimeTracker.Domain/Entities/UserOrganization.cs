namespace TimeTracker.Domain.Entities;

/// <summary>
/// Membership linking a user to an organization, with their role in that org and
/// whether it is the default org pre-selected after login. Table: <c>t_user_organization</c>.
/// </summary>
public class UserOrganization : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>The user's role in this org (null until assigned).</summary>
    public int? OrganizationRoleId { get; set; }

    /// <summary>Whether this org is pre-selected when the user logs in.</summary>
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public OrganizationRole? OrganizationRole { get; set; }
}
