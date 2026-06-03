using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Lookup of supported field data types. Rows are seeded from <see cref="FieldDataType"/>.
/// Table: <c>t_type_field_data_type</c>.
/// </summary>
public class FieldDataTypeLookup
{
    /// <summary>Primary key; the strongly-typed data type value (stored as int).</summary>
    public FieldDataType Id { get; set; }

    /// <summary>Machine code, e.g. "text", "number", "date", "boolean", "select".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;
}
