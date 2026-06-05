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

    public ICollection<UserOrganization> Organizations { get; set; } = new List<UserOrganization>();
}
