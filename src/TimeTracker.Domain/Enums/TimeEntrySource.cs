namespace TimeTracker.Domain.Enums;

/// <summary>
/// Where a time entry originated. Stored as an int on <c>t_time_entry.source</c>.
/// Defaults to <see cref="Manual"/> for hand-entered logs.
/// </summary>
public enum TimeEntrySource
{
    /// <summary>Entered by hand in the app.</summary>
    Manual = 0,

    /// <summary>Imported from an Outlook/Microsoft 365 calendar via Microsoft Graph.</summary>
    OutlookGraph = 1,
}
