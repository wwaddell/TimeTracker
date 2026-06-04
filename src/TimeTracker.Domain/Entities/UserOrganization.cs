namespace TimeTracker.Domain.Entities;

/// <summary>
/// Membership linking a user to an organization. The user's role(s) in the org are
/// held in <see cref="Roles"/>. Table: <c>t_user_organization</c>.
/// </summary>
public class UserOrganization : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Whether this org is pre-selected when the user logs in.</summary>
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;

    /// <summary>Roles this user holds in the org (zero or more).</summary>
    public ICollection<UserOrganizationRole> Roles { get; set; } = new List<UserOrganizationRole>();
}
