namespace TimeTracker.Contracts.Me;

/// <summary>The current user, as needed by the profile page.</summary>
public record MeDto(
    int Id,
    string DisplayName,
    string Email,
    bool IsGlobalAdmin,
    bool HideOrgSwitcher,
    int? DefaultOrganizationId);

/// <summary>Update the user's personal preferences.</summary>
public record SaveMePreferencesRequest
{
    /// <summary>Which org to flag default (must be one the user belongs to). Null leaves unchanged.</summary>
    public int? DefaultOrganizationId { get; init; }

    /// <summary>Whether to hide the org switcher on Log Time / Tasks.</summary>
    public bool HideOrgSwitcher { get; init; }
}

