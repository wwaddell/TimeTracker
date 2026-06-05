namespace TimeTracker.Contracts.Projects;

/// <summary>An org-level project: per-org, optionally restricted to specific members.</summary>
public record ProjectDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    bool IsRestricted,
    string? ReferenceCode,
    string? ExternalUrl,
    int MemberCount);

/// <summary>Lightweight project entry used in pickers (only what's visible to the current user).</summary>
public record ProjectPickerDto(int Id, string Name, string? ReferenceCode);

/// <summary>Create/update payload for a project.</summary>
public record SaveProjectRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    /// <summary>False = visible to all org members; true = only the explicit member list.</summary>
    public bool IsRestricted { get; init; }

    /// <summary>Short tag used to auto-map calendar imports (e.g. "ACME-001").</summary>
    public string? ReferenceCode { get; init; }

    /// <summary>External URL (e.g. the project's page in another system).</summary>
    public string? ExternalUrl { get; init; }
}

/// <summary>A member who's been granted access to a restricted project.</summary>
public record ProjectMemberDto(int UserId, string DisplayName, string Email, bool IsPending);

/// <summary>Add a member to a restricted project by email (pending user created if new).</summary>
public record AddProjectMemberRequest
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}
