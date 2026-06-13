using SQLite;

namespace TimeTracker.Mobile.Data;

/// <summary>
/// Thin async wrapper over the on-device SQLite database. Owns the connection + schema
/// creation; exposes typed queries the DataService and SyncService build on. One file in
/// the app's data dir, created on first use.
/// </summary>
public class LocalStore
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> ConnAsync()
    {
        if (_db is not null)
        {
            return _db;
        }
        var path = Path.Combine(FileSystem.AppDataDirectory, "timetracker.db3");
        var db = new SQLiteAsyncConnection(path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await db.CreateTableAsync<LocalTimeEntry>();
        await db.CreateTableAsync<LocalTask>();
        await db.CreateTableAsync<LocalOrg>();
        await db.CreateTableAsync<LocalProject>();
        _db = db;
        return db;
    }

    // --- Orgs (reference cache) ---

    public async Task<List<LocalOrg>> GetOrgsAsync()
    {
        var db = await ConnAsync();
        return await db.Table<LocalOrg>().OrderBy(o => o.SortOrder).ThenBy(o => o.Name).ToListAsync();
    }

    public async Task ReplaceOrgsAsync(IEnumerable<LocalOrg> orgs)
    {
        var db = await ConnAsync();
        await db.DeleteAllAsync<LocalOrg>();
        await db.InsertAllAsync(orgs);
    }

    // --- Projects (reference cache, per org) ---

    public async Task<List<LocalProject>> GetProjectsAsync(int orgId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalProject>().Where(p => p.OrgId == orgId).OrderBy(p => p.Name).ToListAsync();
    }

    public async Task ReplaceProjectsAsync(int orgId, IEnumerable<LocalProject> projects)
    {
        var db = await ConnAsync();
        await db.Table<LocalProject>().Where(p => p.OrgId == orgId).DeleteAsync();
        await db.InsertAllAsync(projects);
    }

    // --- Time entries ---

    /// <summary>Visible entries for the org (excludes local delete tombstones), newest-first.</summary>
    public async Task<List<LocalTimeEntry>> GetTimeEntriesAsync(int orgId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTimeEntry>()
            .Where(e => e.OrgId == orgId && e.SyncStatus != SyncStatus.Deleted)
            .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.UpdatedTicks)
            .ToListAsync();
    }

    public async Task<LocalTimeEntry?> GetTimeEntryAsync(string localId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTimeEntry>().Where(e => e.LocalId == localId).FirstOrDefaultAsync();
    }

    public async Task UpsertTimeEntryAsync(LocalTimeEntry entry)
    {
        var db = await ConnAsync();
        await db.InsertOrReplaceAsync(entry);
    }

    public async Task DeleteTimeEntryRowAsync(string localId)
    {
        var db = await ConnAsync();
        await db.Table<LocalTimeEntry>().Where(e => e.LocalId == localId).DeleteAsync();
    }

    public async Task<List<LocalTimeEntry>> GetPendingTimeEntriesAsync()
    {
        var db = await ConnAsync();
        return await db.Table<LocalTimeEntry>().Where(e => e.SyncStatus != SyncStatus.Synced).ToListAsync();
    }

    /// <summary>Synced rows for an org — pull replaces these wholesale; pending rows are left alone.</summary>
    public async Task DeleteSyncedTimeEntriesAsync(int orgId)
    {
        var db = await ConnAsync();
        await db.Table<LocalTimeEntry>().Where(e => e.OrgId == orgId && e.SyncStatus == SyncStatus.Synced).DeleteAsync();
    }

    public async Task<bool> TimeEntryExistsByServerIdAsync(long serverId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTimeEntry>().Where(e => e.ServerId == serverId).CountAsync() > 0;
    }

    // --- Tasks ---

    public async Task<List<LocalTask>> GetTasksAsync(int orgId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTask>()
            .Where(t => t.OrgId == orgId && t.SyncStatus != SyncStatus.Deleted)
            .ToListAsync();
    }

    public async Task<LocalTask?> GetTaskAsync(string localId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTask>().Where(t => t.LocalId == localId).FirstOrDefaultAsync();
    }

    public async Task UpsertTaskAsync(LocalTask task)
    {
        var db = await ConnAsync();
        await db.InsertOrReplaceAsync(task);
    }

    public async Task DeleteTaskRowAsync(string localId)
    {
        var db = await ConnAsync();
        await db.Table<LocalTask>().Where(t => t.LocalId == localId).DeleteAsync();
    }

    public async Task<List<LocalTask>> GetPendingTasksAsync()
    {
        var db = await ConnAsync();
        return await db.Table<LocalTask>().Where(t => t.SyncStatus != SyncStatus.Synced).ToListAsync();
    }

    public async Task DeleteSyncedTasksAsync(int orgId)
    {
        var db = await ConnAsync();
        await db.Table<LocalTask>().Where(t => t.OrgId == orgId && t.SyncStatus == SyncStatus.Synced).DeleteAsync();
    }

    public async Task<bool> TaskExistsByServerIdAsync(int serverId)
    {
        var db = await ConnAsync();
        return await db.Table<LocalTask>().Where(t => t.ServerId == serverId).CountAsync() > 0;
    }
}
