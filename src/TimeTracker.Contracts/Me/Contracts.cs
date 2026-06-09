namespace TimeTracker.Contracts.Me;

/// <summary>The current user, as needed by the profile page and the nav-hide logic.</summary>
/// <param name="WeekStartsOn">First day of the week (Sunday=0..Saturday=6); drives "group by week" totals.</param>
/// <param name="OrgRights">
/// Per-org rights the user holds, by organization id → list of OrgRight enum names
/// (ManageOrganization, ManageUsers, ManageRoles, ManageFields, ViewReports, ManageProjects).
/// Strings instead of the enum keep this contract decoupled from the Domain assembly.
/// Global admins get every right pre-populated for every org they belong to.
/// </param>
public record MeDto(
    int Id,
    string DisplayName,
    string Email,
    bool IsGlobalAdmin,
    bool HideOrgSwitcher,
    bool DarkMode,
    bool CompactMode,
    int WeekStartsOn,
    int? DefaultOrganizationId,
    IReadOnlyDictionary<int, IReadOnlyList<string>> OrgRights);

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

    /// <summary>First day of the week (Sunday=0..Saturday=6); validated against valid DayOfWeek.</summary>
    public int? WeekStartsOn { get; init; }
}
