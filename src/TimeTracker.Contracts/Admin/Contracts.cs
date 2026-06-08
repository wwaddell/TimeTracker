using TimeTracker.Domain.Enums;

namespace TimeTracker.Contracts.Admin;

/// <summary>A role within an organization (for scoping fields).</summary>
public record OrganizationRoleDto(int Id, string Name);

/// <summary>An option for a select-type field; used for both input and output.</summary>
public record EntryFieldOptionInput(string Value, string Label, int SortOrder, string? Icon = null);

/// <summary>Full admin view of a configurable field, including inactive ones.</summary>
/// <param name="DefaultValue">Stringified default; interpretation depends on DataType.
/// Boolean: "true"/"false"/null. Select: must match one of Options[].Value. Number/Date/Text: literal.</param>
public record EntryFieldAdminDto(
    int Id,
    string FieldKey,
    string Label,
    FieldDataType DataType,
    bool IsRequired,
    int SortOrder,
    bool IsActive,
    int? OrganizationRoleId,
    string? OrganizationRoleName,
    string? DefaultValue,
    IReadOnlyList<EntryFieldOptionInput> Options);

/// <summary>Create/update payload for a configurable field.</summary>
public record SaveEntryFieldRequest
{
    public string FieldKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public FieldDataType DataType { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Null = applies to all roles in the org; otherwise scoped to one role.</summary>
    public int? OrganizationRoleId { get; init; }

    /// <summary>Allowed values; only meaningful for <see cref="FieldDataType.Select"/>.</summary>
    public List<EntryFieldOptionInput> Options { get; init; } = new();

    /// <summary>
    /// Pre-fill on new entries when the user hasn't touched this field. Server validates
    /// per DataType: Boolean = "true"/"false"/null, Select = must match an Option value,
    /// Number = parseable, Text/Date = anything.
    /// </summary>
    public string? DefaultValue { get; init; }
}

/// <summary>Result of a save, echoing the field id and whether it was deactivated vs deleted.</summary>
public record DeleteFieldResult(bool Deleted, bool Deactivated, string Message);
