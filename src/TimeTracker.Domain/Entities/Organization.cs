namespace TimeTracker.Domain.Entities;

/// <summary>An organization a user can track time for. Table: <c>t_organization</c>.</summary>
public class Organization : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationRole> Roles { get; set; } = new List<OrganizationRole>();
    public ICollection<UserOrganization> Members { get; set; } = new List<UserOrganization>();
    public ICollection<TimeEntryField> TimeEntryFields { get; set; } = new List<TimeEntryField>();
}
