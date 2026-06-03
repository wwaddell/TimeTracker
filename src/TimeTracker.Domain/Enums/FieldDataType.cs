namespace TimeTracker.Domain.Enums;

/// <summary>
/// The data type of a configurable time-entry field. Values are stable and
/// mirror the rows seeded into <c>t_type_field_data_type</c>.
/// </summary>
public enum FieldDataType
{
    Text = 1,
    Number = 2,
    Date = 3,
    Boolean = 4,
    Select = 5,
}
