namespace TimeTracker.Web.Services;

/// <summary>
/// Remembers the organization picked in any page's org switcher for the lifetime of the
/// browser session (WASM scoped service ≈ one instance per tab). Without this, every page
/// re-defaulted to the first org on navigation, resetting the user's selection.
/// Deliberately not persisted — the cross-session default lives in the user's
/// DefaultOrganizationId preference instead.
/// </summary>
public class OrgSelectionState
{
    public int? OrgId { get; set; }

    /// <summary>
    /// The remembered org if it's still in the caller's visible set; otherwise the
    /// caller's fallback (typically the first/default org). Guards against a stale
    /// remembered id, e.g. after being removed from an org mid-session.
    /// </summary>
    public int Resolve(IEnumerable<int> validIds, int fallback) =>
        OrgId is { } id && validIds.Contains(id) ? id : fallback;
}
