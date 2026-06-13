using TimeTracker.Mobile.Data;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// The single data API the pages use. Always reads from the local SQLite cache (so the UI
/// works offline) and writes locally first, marking rows dirty for the SyncService to push.
/// When online, writes trigger a background sync so changes propagate promptly; when
/// offline they queue until connectivity returns.
/// </summary>
public class DataService
{
    private readonly LocalStore _store;
    private readonly SyncService _sync;

    public DataService(LocalStore store, SyncService sync)
    {
        _store = store;
        _sync = sync;
        // Auto-sync the moment the device regains connectivity.
        Connectivity.Current.ConnectivityChanged += async (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet && LastOrgId != 0)
            {
                await _sync.SyncAsync(LastOrgId);
            }
        };
    }

    public static bool IsOnline => SyncService.IsOnline;

    /// <summary>Remembered so connectivity-triggered syncs know which org to pull.</summary>
    public int LastOrgId { get; set; }

    /// <summary>Fires after a background sync updates local data; pages re-query on this.</summary>
    public event Action? Changed
    {
        add => _sync.Changed += value;
        remove => _sync.Changed -= value;
    }

    public Task<bool> SyncAsync(int orgId)
    {
        LastOrgId = orgId;
        return _sync.SyncAsync(orgId);
    }

    // --- Reads (always local) ---

    public Task<List<LocalOrg>> GetOrgsAsync() => _store.GetOrgsAsync();
    public Task<List<LocalProject>> GetProjectsAsync(int orgId) => _store.GetProjectsAsync(orgId);
    public Task<List<LocalTimeEntry>> GetTimeEntriesAsync(int orgId) => _store.GetTimeEntriesAsync(orgId);
    public Task<List<LocalTask>> GetTasksAsync(int orgId) => _store.GetTasksAsync(orgId);

    /// <summary>Count of rows waiting to push — drives the "unsynced" UI hint.</summary>
    public async Task<int> PendingCountAsync() =>
        (await _store.GetPendingTimeEntriesAsync()).Count + (await _store.GetPendingTasksAsync()).Count;

    // --- Time entry writes ---

    public async Task SaveTimeEntryAsync(LocalTimeEntry entry)
    {
        entry.SyncStatus = await NextStatusAsync(await _store.GetTimeEntryAsync(entry.LocalId), entry.SyncStatus);
        entry.UpdatedTicks = DateTime.UtcNow.Ticks;
        await _store.UpsertTimeEntryAsync(entry);
        await TrySyncAsync(entry.OrgId);
    }

    public async Task DeleteTimeEntryAsync(LocalTimeEntry entry)
    {
        // A create that never synced has no server row — just drop it locally.
        if (entry.ServerId is null)
        {
            await _store.DeleteTimeEntryRowAsync(entry.LocalId);
        }
        else
        {
            entry.SyncStatus = SyncStatus.Deleted;
            entry.UpdatedTicks = DateTime.UtcNow.Ticks;
            await _store.UpsertTimeEntryAsync(entry);
        }
        await TrySyncAsync(entry.OrgId);
    }

    // --- Task writes ---

    public async Task SaveTaskAsync(LocalTask task)
    {
        task.SyncStatus = await NextStatusAsync(await _store.GetTaskAsync(task.LocalId), task.SyncStatus);
        task.UpdatedTicks = DateTime.UtcNow.Ticks;
        await _store.UpsertTaskAsync(task);
        await TrySyncAsync(task.OrgId);
    }

    public async Task DeleteTaskAsync(LocalTask task)
    {
        if (task.ServerId is null)
        {
            await _store.DeleteTaskRowAsync(task.LocalId);
        }
        else
        {
            task.SyncStatus = SyncStatus.Deleted;
            task.UpdatedTicks = DateTime.UtcNow.Ticks;
            await _store.UpsertTaskAsync(task);
        }
        await TrySyncAsync(task.OrgId);
    }

    // Decide the post-save sync status: a brand-new row is Created; editing a row that
    // hasn't synced its create keeps it Created (one POST sends the final state); editing
    // an already-synced row is an Updated (PUT).
    private static Task<SyncStatus> NextStatusAsync<T>(T? existing, SyncStatus _)
        where T : class
    {
        var status = existing switch
        {
            null => SyncStatus.Created,
            LocalTimeEntry { SyncStatus: SyncStatus.Created } => SyncStatus.Created,
            LocalTask { SyncStatus: SyncStatus.Created } => SyncStatus.Created,
            _ => SyncStatus.Updated,
        };
        return Task.FromResult(status);
    }

    // Fire-and-forget sync after a write (only does work when online).
    private async Task TrySyncAsync(int orgId)
    {
        LastOrgId = orgId;
        if (IsOnline)
        {
            await _sync.SyncAsync(orgId);
        }
    }
}
