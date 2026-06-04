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
