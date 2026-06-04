using TimeTracker.Domain.Enums;

namespace TimeTracker.Contracts.Roles;

/// <summary>Full admin view of a role: its rights and how many members hold it.</summary>
public record RoleAdminDto(
    int Id,
    string Name,
    int SortOrder,
    int MemberCount,
    IReadOnlyList<OrgRight> Rights);

/// <summary>Create/update payload for a role.</summary>
public record SaveRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public List<OrgRight> Rights { get; init; } = new();
}

/// <summary>A user who holds a given role.</summary>
public record RoleMemberDto(int UserId, string Email, string DisplayName);
