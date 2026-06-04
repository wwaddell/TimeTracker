namespace TimeTracker.Domain.Entities;

/// <summary>
/// Assigns one organization role to a membership. A user may hold several roles in an
/// org; their effective rights are the union. Table: <c>t_user_organization_role</c>.
/// </summary>
public class UserOrganizationRole
{
    public int Id { get; set; }
    public int UserOrganizationId { get; set; }
    public int OrganizationRoleId { get; set; }

    public UserOrganization UserOrganization { get; set; } = null!;
    public OrganizationRole OrganizationRole { get; set; } = null!;
}
