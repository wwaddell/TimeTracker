using TimeTracker.Domain.Enums;

namespace TimeTracker.Contracts.Rights;

/// <summary>A grantable organization right from the fixed catalog.</summary>
public record RightDto(OrgRight Id, string Code, string Name, string Description);
