using TimeTracker.Contracts.Me;
using TimeTracker.Contracts.TimeEntries;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// Singleton holding session state shared across pages: the signed-in user, the orgs they
/// belong to, and the currently-selected org (sticky while the app is open, like the web
/// app's OrgSelectionState). Pages read/write this instead of re-fetching on every nav.
/// </summary>
public class AppState
{
    public MeDto? Me { get; set; }
    public IReadOnlyList<OrganizationDto> Organizations { get; set; } = [];
    public int SelectedOrgId { get; set; }

    public OrganizationDto? SelectedOrg =>
        Organizations.FirstOrDefault(o => o.Id == SelectedOrgId);

    /// <summary>Pick a sensible default org: the user's default if set + present, else the first.</summary>
    public void EnsureSelection()
    {
        if (Organizations.Count == 0)
        {
            SelectedOrgId = 0;
            return;
        }
        if (SelectedOrgId != 0 && Organizations.Any(o => o.Id == SelectedOrgId))
        {
            return;
        }
        var preferred = Me?.DefaultOrganizationId;
        SelectedOrgId = preferred is { } p && Organizations.Any(o => o.Id == p)
            ? p
            : Organizations[0].Id;
    }
}
