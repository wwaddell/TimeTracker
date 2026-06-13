using TimeTracker.Contracts.Tasks;
using TimeTracker.Contracts.TimeEntries;
using TimeTracker.Domain.Enums;
using TimeTracker.Mobile.Data;

namespace TimeTracker.Mobile.Services;

/// <summary>
/// Reconciles the local SQLite cache with the server. Push-then-pull, last-write-wins:
/// pending local rows are sent first (so a device edit overwrites the server), then fresh
/// server data replaces the *synced* local rows (pending rows are left for the next push).
/// All operations are no-ops when offline.
/// </summary>
public class SyncService(ApiClient api, LocalStore store)
{
    public static bool IsOnline =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    /// <summary>Raised after a sync changes local data, so open pages can refresh.</summary>
    public event Action? Changed;

    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Full sync for one org: push all pending rows (any org), then pull this org's data.
    /// Returns false if offline or already running. Swallows per-row push failures (they
    /// stay pending and retry next time) so one bad row can't block the rest.
    /// </summary>
    public async Task<bool> SyncAsync(int orgId)
    {
        if (!IsOnline || !await _gate.WaitAsync(0))
        {
            return false;
        }
        try
        {
            await PushTimeEntriesAsync();
            await PushTasksAsync();
            await PullAsync(orgId);
            Changed?.Invoke();
            return true;
        }
        catch (ApiException)
        {
            // Network blipped or auth expired mid-sync — leave local state, try again later.
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    // --- Push ---

    private async Task PushTimeEntriesAsync()
    {
        foreach (var e in await store.GetPendingTimeEntriesAsync())
        {
            try
            {
                switch (e.SyncStatus)
                {
                    case SyncStatus.Created:
                        e.ServerId = await api.CreateTimeEntryAsync(e.OrgId, ToEntryRequest(e));
                        e.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTimeEntryAsync(e);
                        break;
                    case SyncStatus.Updated when e.ServerId is { } sid:
                        await api.UpdateTimeEntryAsync(e.OrgId, sid, ToEntryRequest(e));
                        e.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTimeEntryAsync(e);
                        break;
                    case SyncStatus.Updated:
                        // Edited before its create ever synced — treat as a create.
                        e.ServerId = await api.CreateTimeEntryAsync(e.OrgId, ToEntryRequest(e));
                        e.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTimeEntryAsync(e);
                        break;
                    case SyncStatus.Deleted:
                        if (e.ServerId is { } delId)
                        {
                            await api.DeleteTimeEntryAsync(e.OrgId, delId);
                        }
                        await store.DeleteTimeEntryRowAsync(e.LocalId); // drop the tombstone
                        break;
                }
            }
            catch (ApiException)
            {
                // Leave this row pending; continue with the rest.
            }
        }
    }

    private async Task PushTasksAsync()
    {
        foreach (var t in await store.GetPendingTasksAsync())
        {
            try
            {
                switch (t.SyncStatus)
                {
                    case SyncStatus.Created:
                        t.ServerId = await api.CreateTaskAsync(t.OrgId, ToTaskRequest(t));
                        t.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTaskAsync(t);
                        break;
                    case SyncStatus.Updated when t.ServerId is { } sid:
                        await api.UpdateTaskAsync(t.OrgId, sid, ToTaskRequest(t));
                        t.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTaskAsync(t);
                        break;
                    case SyncStatus.Updated:
                        t.ServerId = await api.CreateTaskAsync(t.OrgId, ToTaskRequest(t));
                        t.SyncStatus = SyncStatus.Synced;
                        await store.UpsertTaskAsync(t);
                        break;
                    case SyncStatus.Deleted:
                        // No task DELETE in the API yet; mark complete instead, then drop the tombstone.
                        if (t.ServerId is { } sid2)
                        {
                            var req = ToTaskRequest(t) with { IsComplete = true, PercentComplete = 100 };
                            await api.UpdateTaskAsync(t.OrgId, sid2, req);
                        }
                        await store.DeleteTaskRowAsync(t.LocalId);
                        break;
                }
            }
            catch (ApiException)
            {
            }
        }
    }

    // --- Pull ---

    private async Task PullAsync(int orgId)
    {
        var orgs = await api.GetOrganizationsAsync();
        await store.ReplaceOrgsAsync(orgs.Select((o, i) => new LocalOrg
        {
            Id = o.Id,
            Name = o.Name,
            RequireTime = o.RequireTime,
            RequireProject = o.RequireProject,
            SortOrder = i,
        }));

        if (orgId == 0)
        {
            return;
        }

        var projects = await api.GetVisibleProjectsAsync(orgId);
        await store.ReplaceProjectsAsync(orgId, projects.Select(p => new LocalProject
        {
            Id = p.Id,
            OrgId = orgId,
            Name = p.Name,
            ReferenceCode = p.ReferenceCode,
        }));

        // Replace only synced rows; pending local edits/creates survive and push next time.
        var entries = await api.GetTimeEntriesAsync(orgId, 1, 200);
        await store.DeleteSyncedTimeEntriesAsync(orgId);
        foreach (var e in entries.Items)
        {
            // Skip server rows that a still-pending local row already represents.
            if (await store.TimeEntryExistsByServerIdAsync(e.Id))
            {
                continue;
            }
            await store.UpsertTimeEntryAsync(FromEntryDto(e, orgId));
        }

        var tasks = await api.GetTasksAsync(orgId, scope: "all");
        await store.DeleteSyncedTasksAsync(orgId);
        foreach (var t in tasks)
        {
            if (await store.TaskExistsByServerIdAsync(t.Id))
            {
                continue;
            }
            await store.UpsertTaskAsync(FromTaskDto(t, orgId));
        }
    }

    // --- Mapping ---

    private static CreateTimeEntryRequest ToEntryRequest(LocalTimeEntry e) => new()
    {
        Note = e.Note,
        EntryDate = DateOnly.Parse(e.EntryDate),
        DurationMinutes = e.DurationMinutes,
        ProjectId = e.ProjectId,
        TaskId = e.TaskId,
        Timezone = e.Timezone,
    };

    private static LocalTimeEntry FromEntryDto(TimeEntryDto e, int orgId) => new()
    {
        ServerId = e.Id,
        OrgId = orgId,
        Note = e.Note,
        EntryDate = e.EntryDate.ToString("yyyy-MM-dd"),
        DurationMinutes = e.DurationMinutes,
        ProjectId = e.ProjectId,
        ProjectName = e.ProjectName,
        TaskId = e.TaskId,
        TaskTitle = e.TaskTitle,
        SyncStatus = SyncStatus.Synced,
        UpdatedTicks = e.CreatedUtc.Ticks,
    };

    private static SaveTaskRequest ToTaskRequest(LocalTask t) => new()
    {
        Title = t.Title,
        Description = t.Description,
        IsComplete = t.IsComplete,
        PercentComplete = t.PercentComplete,
        PercentBeforeComplete = t.PercentBeforeComplete,
        Priority = t.Priority is { } p ? (TaskPriority)p : null,
        DueDate = string.IsNullOrEmpty(t.DueDate) ? null : DateOnly.Parse(t.DueDate),
        ProjectId = t.ProjectId,
        ReferenceCode = t.ReferenceCode,
        ExternalUrl = t.ExternalUrl,
        AssignedToUserId = t.AssignedToUserId,
    };

    private static LocalTask FromTaskDto(TaskDto t, int orgId) => new()
    {
        ServerId = t.Id,
        OrgId = orgId,
        Title = t.Title,
        Description = t.Description,
        IsComplete = t.IsComplete,
        PercentComplete = t.PercentComplete,
        PercentBeforeComplete = t.PercentBeforeComplete,
        Priority = t.Priority is { } p ? (int)p : null,
        DueDate = t.DueDate?.ToString("yyyy-MM-dd"),
        ProjectId = t.ProjectId,
        ProjectName = t.ProjectName,
        ReferenceCode = t.ReferenceCode,
        ExternalUrl = t.ExternalUrl,
        AssignedToUserId = t.AssignedToUserId,
        AssignedToName = t.AssignedToName,
        SyncStatus = SyncStatus.Synced,
        UpdatedTicks = t.CreatedUtc.Ticks,
    };
}
