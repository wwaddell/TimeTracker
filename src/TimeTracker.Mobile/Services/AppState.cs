using TimeTracker.Mobile.Data;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// Session state shared across pages: the orgs the user belongs to (from the local cache,
/// so available offline) and the sticky selected org. The selection persists across app
/// launches via Preferences.
/// </summary>
public class AppState
{
    private const string SelectedOrgKey = "tt.selectedOrgId";

    public IReadOnlyList<LocalOrg> Organizations { get; private set; } = [];

    public int SelectedOrgId
    {
        get => Preferences.Get(SelectedOrgKey, 0);
        set => Preferences.Set(SelectedOrgKey, value);
    }

    public LocalOrg? SelectedOrg => Organizations.FirstOrDefault(o => o.Id == SelectedOrgId);

    /// <summary>Refresh the org list from the local cache and make sure a valid org is selected.</summary>
    public async Task LoadOrgsAsync(DataService data)
    {
        Organizations = await data.GetOrgsAsync();
        if (Organizations.Count == 0)
        {
            SelectedOrgId = 0;
            return;
        }
        if (SelectedOrgId == 0 || Organizations.All(o => o.Id != SelectedOrgId))
        {
            SelectedOrgId = Organizations[0].Id;
        }
    }
}
