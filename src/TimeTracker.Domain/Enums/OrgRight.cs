namespace TimeTracker.Domain.Enums;

/// <summary>
/// A capability that can be granted to an organization role. Values are stable and
/// mirror the rows seeded into <c>t_type_right</c>. Any active member may log time;
/// these rights gate administrative actions.
/// </summary>
public enum OrgRight
{
    ManageOrganization = 1,
    ManageUsers = 2,
    ManageRoles = 3,
    ManageFields = 4,
    ViewReports = 5,
}
