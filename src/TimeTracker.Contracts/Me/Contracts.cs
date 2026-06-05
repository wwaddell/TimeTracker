namespace TimeTracker.Contracts.Me;

/// <summary>The current user, as needed by the profile page.</summary>
public record MeDto(
    int Id,
    string DisplayName,
    string Email,
    bool IsGlobalAdmin,
    bool HideOrgSwitcher,
    bool DarkMode,
    bool CompactMode,
    int? DefaultOrganizationId);

/// <summary>
/// Update the user's personal preferences. All fields are optional — null means
/// "leave unchanged" — so callers can save individual toggles without having to
/// know (or send) every other preference.
/// </summary>
public record SaveMePreferencesRequest
{
    /// <summary>Which org to flag default (must be one the user belongs to).</summary>
    public int? DefaultOrganizationId { get; init; }

    /// <summary>Whether to hide the org switcher on Log Time / Tasks / Import.</summary>
    public bool? HideOrgSwitcher { get; init; }

    /// <summary>Dark-mode preference.</summary>
    public bool? DarkMode { get; init; }

    /// <summary>Compact (hint-free) layout for the Log Time page.</summary>
    public bool? CompactMode { get; init; }
}
