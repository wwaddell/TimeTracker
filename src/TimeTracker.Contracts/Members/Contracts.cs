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

    /// <summary>
    /// When true, the server fires the invite-email side effect (currently stubbed — no SMTP
    /// integration yet). Default true so the invite flow stays opt-out rather than opt-in.
    /// </summary>
    public bool SendInvite { get; init; } = true;
}

/// <summary>Replace the set of roles a member holds.</summary>
public record SetMemberRolesRequest
{
    public List<int> RoleIds { get; init; } = new();
}

/// <summary>
/// Update a member's profile (DisplayName + Email) and optionally fire the invite email
/// again — useful when an admin corrects a typo or has the user re-receive credentials.
/// Both DisplayName and Email are required; this is an authoritative replace.
/// </summary>
public record UpdateMemberProfileRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Trigger the (stubbed) invite-email side effect after saving. Default false on edit so
    /// admins changing a name don't accidentally re-mail the user.
    /// </summary>
    public bool SendInvite { get; init; }
}
