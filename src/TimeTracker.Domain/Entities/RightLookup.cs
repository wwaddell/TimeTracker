using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Lookup of the fixed catalog of organization rights. Rows are seeded from
/// <see cref="OrgRight"/>. Table: <c>t_type_right</c>.
/// </summary>
public class RightLookup
{
    /// <summary>Primary key; the strongly-typed right value (stored as int).</summary>
    public OrgRight Id { get; set; }

    /// <summary>Machine code, e.g. "manage_users".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description of what the right allows.</summary>
    public string Description { get; set; } = string.Empty;
}
