namespace TimeTracker.Contracts.Members;

/// <summary>A role reference (id + name) held by a member.</summary>
public record RoleRefDto(int Id, string Name);

/// <summary>A member of an organization and the roles they hold.</summary>
public record MemberDto(
    int UserId,
    string Email,
    string DisplayName,
    bool IsPending,
    bool IsDefault,
    IReadOnlyList<RoleRefDto> Roles);

/// <summary>Invite/add a member by email with an initial set of roles.</summary>
public record InviteMemberRequest
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public List<int> RoleIds { get; init; } = new();
}

/// <summary>Replace the set of roles a member holds.</summary>
public record SetMemberRolesRequest
{
    public List<int> RoleIds { get; init; } = new();
}
