using SQLite;

namespace TimeTracker.Mobile.Data;

/// <summary>
/// Sync state of a locally-stored row.
/// - Synced: matches the server (safe to overwrite on pull).
/// - Created: made offline, never pushed (POST on sync; has no ServerId yet).
/// - Updated: edited locally after being synced (PUT on sync).
/// - Deleted: deleted locally; tombstone kept until the server DELETE confirms.
/// </summary>
public enum SyncStatus
{
    Synced = 0,
    Created = 1,
    Updated = 2,
    Deleted = 3,
}

/// <summary>
/// A time entry held on the device. LocalId (a GUID) is the stable identity used by the
/// UI so offline-created rows are addressable before they have a ServerId. EntryDate is
/// stored as an ISO yyyy-MM-dd string because sqlite-net has no DateOnly mapping.
/// </summary>
public class LocalTimeEntry
{
    [PrimaryKey] public string LocalId { get; set; } = Guid.NewGuid().ToString("n");
    [Indexed] public long? ServerId { get; set; }
    [Indexed] public int OrgId { get; set; }

    public string Note { get; set; } = string.Empty;
    public string EntryDate { get; set; } = string.Empty; // yyyy-MM-dd
    public int? DurationMinutes { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public string? Timezone { get; set; }

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;
    /// <summary>Local last-modified, for newest-first ordering and tie-breaks.</summary>
    public long UpdatedTicks { get; set; }

    // --- Display helpers (not persisted) ---
    [Ignore] public bool IsUnsynced => SyncStatus != SyncStatus.Synced;
    [Ignore] public string DateDisplay =>
        DateOnly.TryParse(EntryDate, out var d) ? d.ToString("ddd, MMM d") : EntryDate;
    [Ignore] public string DurationDisplay => DurationMinutes is { } m ? $"{m} min" : "";
}

/// <summary>A task held on the device. See <see cref="LocalTimeEntry"/> for the id/date conventions.</summary>
public class LocalTask
{
    [PrimaryKey] public string LocalId { get; set; } = Guid.NewGuid().ToString("n");
    [Indexed] public int? ServerId { get; set; }
    [Indexed] public int OrgId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public int PercentComplete { get; set; }
    public int? PercentBeforeComplete { get; set; }
    public int? Priority { get; set; }       // TaskPriority as int (1..4), null = none
    public string? DueDate { get; set; }     // yyyy-MM-dd
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ReferenceCode { get; set; }
    public string? ExternalUrl { get; set; }
    public int AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;
    public long UpdatedTicks { get; set; }

    [Ignore] public bool IsUnsynced => SyncStatus != SyncStatus.Synced;
}

/// <summary>Read-only cached organization the user belongs to (refreshed on pull).</summary>
public class LocalOrg
{
    [PrimaryKey] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool RequireTime { get; set; } = true;
    public bool RequireProject { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Read-only cached project (visible-to-me set), keyed globally; OrgId filters per org.</summary>
public class LocalProject
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
}
