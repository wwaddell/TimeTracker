using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Definition of a configurable extra field collected on time entries for an
/// organization, optionally narrowed to a single role. This is what an org admin
/// edits. Table: <c>t_time_entry_field</c>.
/// </summary>
public class TimeEntryField : AuditableEntity
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>When set, the field applies only to this role; null = all roles in the org.</summary>
    public int? OrganizationRoleId { get; set; }

    /// <summary>Stable machine key for the field (unique within the org).</summary>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>Human-readable label shown on the entry form.</summary>
    public string Label { get; set; } = string.Empty;

    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
    public OrganizationRole? OrganizationRole { get; set; }
    public FieldDataTypeLookup DataTypeLookup { get; set; } = null!;
    public ICollection<TimeEntryFieldOption> Options { get; set; } = new List<TimeEntryFieldOption>();
}
