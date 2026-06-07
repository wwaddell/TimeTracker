namespace TimeTracker.Domain.Entities;

/// <summary>
/// Application-side user. Authentication is handled by the external identity
/// provider (Entra External ID); <see cref="ExternalId"/> stores the stable
/// <c>oid</c>/<c>sub</c> claim. No credentials are stored here. Table: <c>t_user</c>.
/// </summary>
public class User : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Stable subject identifier from the identity provider (oid/sub claim). Null for a
    /// user who was invited by email but has not signed in yet (linked on first login).
    /// </summary>
    public string? ExternalId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Platform-level administrator: can create/manage organizations and assign their admins.</summary>
    public bool IsGlobalAdmin { get; set; }

    /// <summary>
    /// When true, the org switcher is hidden on Log Time/Tasks and the user's default org
    /// (the membership flagged <see cref="UserOrganization.IsDefault"/>) is used.
    /// </summary>
    public bool HideOrgSwitcher { get; set; }

    /// <summary>Dark-mode preference. False = light, true = dark.</summary>
    public bool DarkMode { get; set; }

    /// <summary>When true, the Log Time page is shown in a denser, hint-free layout.</summary>
    public bool CompactMode { get; set; }

    /// <summary>
    /// When true, the browser is asked for the user's location each time they create a time
    /// entry; the lat/lng/accuracy are stored on the entry (hidden from the UI). Off by default;
    /// the browser still prompts for OS-level permission the first time. Used for travel/audit
    /// reports and "where did I do this" analysis.
    /// </summary>
    public bool CaptureLocation { get; set; }

    /// <summary>
    /// First day of the week for "group by week" totals on Log Time. Defaults to Sunday (US).
    /// Stored as the .NET <see cref="DayOfWeek"/> int (Sunday=0..Saturday=6).
    /// </summary>
    public DayOfWeek WeekStartsOn { get; set; } = DayOfWeek.Sunday;

    public ICollection<UserOrganization> Organizations { get; set; } = new List<UserOrganization>();
}
