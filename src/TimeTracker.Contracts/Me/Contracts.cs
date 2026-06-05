namespace TimeTracker.Contracts.Me;

/// <summary>The current user, as needed by the profile page.</summary>
public record MeDto(int Id, string DisplayName, string Email, bool IsGlobalAdmin);
