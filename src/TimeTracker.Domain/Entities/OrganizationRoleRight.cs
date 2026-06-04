using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Grants one right to an organization role. Table: <c>t_organization_role_right</c>.
/// </summary>
public class OrganizationRoleRight
{
    public int Id { get; set; }
    public int OrganizationRoleId { get; set; }
    public OrgRight Right { get; set; }

    public OrganizationRole OrganizationRole { get; set; } = null!;
    public RightLookup RightLookup { get; set; } = null!;
}
