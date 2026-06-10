namespace TimeTracker.Contracts.Reports;

// Report rows are flat, display-ready records: the Reports page renders them in a
// generic table (columns from property names) and exports them verbatim as JSON/CSV,
// so the shapes here ARE the export format. Keep names human-friendly.

/// <summary>
/// Report response envelope. Rows are capped server-side (see ReportEndpoints.MaxRows);
/// <paramref name="Truncated"/> tells the client the cap was hit so it can prompt the
/// user to narrow the parameters rather than silently presenting a partial export.
/// </summary>
public record ReportResult<TRow>(IReadOnlyList<TRow> Rows, bool Truncated);

/// <summary>One time entry in the detail report.</summary>
public record TimeDetailRow(
    DateOnly Date,
    string Person,
    string? Project,
    string? Task,
    string Note,
    int? Minutes);

/// <summary>Total time per project in a range. "(No project)" groups unassigned entries.</summary>
public record TimeByProjectRow(
    string Project,
    int Entries,
    int Minutes,
    decimal Hours);

/// <summary>Total time per member in a range.</summary>
public record TimeByPersonRow(
    string Person,
    int Entries,
    int Minutes,
    decimal Hours);

/// <summary>Total time per day/week/month bucket in a range.</summary>
public record TimeByPeriodRow(
    string Period,
    int Entries,
    int Minutes,
    decimal Hours);

/// <summary>A task completed in the range (per the audit history's IsComplete flip).</summary>
public record TaskCompletedRow(
    string Title,
    string AssignedTo,
    string? Project,
    DateTime CompletedUtc,
    decimal? EstimatedHours,
    decimal ActualHours);

/// <summary>An open (incomplete) task at report time.</summary>
public record OpenTaskRow(
    string Title,
    string AssignedTo,
    string? Project,
    string? Priority,
    DateOnly? DueDate,
    int PercentComplete,
    int AgeDays,
    bool Overdue);

/// <summary>A time entry missing one or more required configurable fields.</summary>
public record IncompleteEntryRow(
    DateOnly Date,
    string Person,
    string Note,
    string MissingFields);
