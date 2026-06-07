namespace TimeTracker.Domain.Entities;

/// <summary>An organization a user can track time for. Table: <c>t_organization</c>.</summary>
public class Organization : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When true, entries capture a start time; when false, date-only (time hidden).</summary>
    public bool RequireTime { get; set; } = true;

    /// <summary>
    /// When true, the browser is asked for the user's location every time a member creates a
    /// time entry for this org; lat/lng/accuracy are stored on the entry (hidden from the UI).
    /// Browser still prompts for OS-level permission the first time. Per-org because location
    /// tracking is a policy decision the org makes, not a personal preference.
    /// </summary>
    public bool CaptureLocation { get; set; }

    public ICollection<OrganizationRole> Roles { get; set; } = new List<OrganizationRole>();
    public ICollection<UserOrganization> Members { get; set; } = new List<UserOrganization>();
    public ICollection<TimeEntryField> TimeEntryFields { get; set; } = new List<TimeEntryField>();
}
