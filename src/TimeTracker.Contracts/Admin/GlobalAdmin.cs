namespace TimeTracker.Contracts.Admin;

/// <summary>An organization as seen by a global (platform) admin.</summary>
public record AdminOrgDto(int Id, string Name, string? Description, bool IsActive, int MemberCount);

/// <summary>Create a new organization.</summary>
public record CreateOrganizationRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>Update an organization's details.</summary>
public record UpdateOrganizationRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>An administrator of an organization (holds a role granting ManageOrganization).</summary>
public record OrgAdminDto(int UserId, string DisplayName, string Email, bool IsPending);

/// <summary>Assign someone as an admin of an organization (by email; created pending if new).</summary>
public record AssignOrgAdminRequest
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}

/// <summary>
/// A user as seen by a global (platform) admin — system-wide, not scoped to a particular
/// organization. <see cref="IsPending"/> means the user was invited by email but hasn't
/// signed in yet (no external_id). <see cref="OrgCount"/> shows how many orgs the user
/// belongs to so admins can spot rogue / orphan accounts.
/// </summary>
public record AdminUserDto(
    int Id,
    string DisplayName,
    string Email,
    bool IsPending,
    bool IsGlobalAdmin,
    int OrgCount);

/// <summary>Edit a user's profile + platform-level flags as a global admin.</summary>
public record UpdateAdminUserRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsGlobalAdmin { get; init; }
}
