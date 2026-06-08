namespace TimeTracker.Contracts.Organizations;

/// <summary>Editable organization details.</summary>
public record OrganizationDetailsDto(int Id, string Name, string? Description, bool IsActive);

/// <summary>Update payload for organization details.</summary>
public record SaveOrganizationRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Entry-form settings configured on the Fields screen.</summary>
public record EntrySettingsRequest
{
    public bool RequireTime { get; init; } = true;

    /// <summary>
    /// When true, the browser is asked for location on every new time entry in this org and
    /// the coords are stored on the entry (hidden from the UI). Off by default.
    /// </summary>
    public bool CaptureLocation { get; init; }

    /// <summary>
    /// When true, time entries must link to a Project. API rejects entries without a
    /// project_id; the entry form marks the picker as required. Off by default.
    /// </summary>
    public bool RequireProject { get; init; }
}
