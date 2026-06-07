using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Time Tracker database. Entity-to-table mapping
/// (<c>t_*</c> / <c>t_type_*</c>) lives in the per-entity configuration classes
/// in <c>Persistence/Configurations</c>. Column names are converted to snake_case
/// by a global convention below.
/// </summary>
public class TimeTrackerDbContext : DbContext
{
    private readonly ICurrentUserProvider _currentUserProvider;

    public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options)
        : this(options, new NullCurrentUserProvider()) { }

    public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options, ICurrentUserProvider currentUserProvider)
        : base(options)
    {
        _currentUserProvider = currentUserProvider;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationRole> OrganizationRoles => Set<OrganizationRole>();
    public DbSet<OrganizationRoleRight> OrganizationRoleRights => Set<OrganizationRoleRight>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();
    public DbSet<UserOrganizationRole> UserOrganizationRoles => Set<UserOrganizationRole>();
    public DbSet<RightLookup> Rights => Set<RightLookup>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TimeEntryField> TimeEntryFields => Set<TimeEntryField>();
    public DbSet<TimeEntryFieldOption> TimeEntryFieldOptions => Set<TimeEntryFieldOption>();
    public DbSet<TimeEntryAttribute> TimeEntryAttributes => Set<TimeEntryAttribute>();
    public DbSet<FieldDataTypeLookup> FieldDataTypes => Set<FieldDataTypeLookup>();
    public DbSet<CalendarSeriesTag> CalendarSeriesTags => Set<CalendarSeriesTag>();
    public DbSet<CalendarSeriesTagAttribute> CalendarSeriesTagAttributes => Set<CalendarSeriesTagAttribute>();
    public DbSet<CalendarConnection> CalendarConnections => Set<CalendarConnection>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskHistory> TaskHistories => Set<TaskHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeTrackerDbContext).Assembly);

        // Soft delete: exclude rows with DeletedUtc set from every query automatically.
        modelBuilder.Entity<TaskItem>().HasQueryFilter(t => t.DeletedUtc == null);
        modelBuilder.Entity<TimeEntry>().HasQueryFilter(e => e.DeletedUtc == null);
        modelBuilder.Entity<Project>().HasQueryFilter(p => p.DeletedUtc == null);

        // Map every column to snake_case based on its CLR property name so the
        // schema matches the house naming style (note, entry_date, organization_id, ...).
        // After snake_case, apply the house PK rule: every Id-style primary key gets the
        // table's core name as a prefix (t_task.id → t_task.task_id, t_type_organization_role.id
        // → t_type_organization_role.organization_role_id). FK column names are unaffected —
        // they keep their property-derived name (user_id on t_time_entry stays user_id, even
        // though it now points at t_user.user_id rather than t_user.id).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            var pk = entityType.FindPrimaryKey();
            if (pk is { Properties.Count: 1 } && pk.Properties[0].Name == "Id")
            {
                var table = entityType.GetTableName();
                if (!string.IsNullOrEmpty(table))
                {
                    pk.Properties[0].SetColumnName($"{StripTablePrefix(table)}_id");
                }
            }
        }
    }

    // t_task → task, t_type_organization_role → organization_role. Used to derive the PK
    // column name from the table name under the house convention (PK = <table>_id).
    private static string StripTablePrefix(string tableName) =>
        tableName.StartsWith("t_type_") ? tableName[7..]
        : tableName.StartsWith("t_") ? tableName[2..]
        : tableName;

    // --- Audit history for TaskItem ---
    //
    // Every save is intercepted so the history rows are written automatically — no endpoint
    // has to remember to call audit logic. The two-pass save is needed because Added entities
    // don't have their generated id until the first SaveChanges completes; we capture the
    // diffs before save, then attach them with the now-known TaskId and save again.

    // Field updates we don't record:
    // - Id: never changes.
    // - CreatedUtc: never changes after Added.
    // - ModifiedUtc: bumps with every save — the history row's ChangedUtc already captures that.
    // - DeletedUtc: surfaces as a synthetic Deleted/Restored marker instead of a raw field change.
    private static readonly HashSet<string> ExcludedFieldsFromAudit =
        new(StringComparer.Ordinal) { nameof(TaskItem.Id), "CreatedUtc", "ModifiedUtc", nameof(TaskItem.DeletedUtc) };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Snapshot of pending TaskItem changes BEFORE save — once we save, OriginalValues snap
        // to CurrentValues and we lose the "what was the old value" information.
        var pendingHistory = CapturePendingTaskHistory();

        // Pending Added TaskItems — we need to record their generated id AFTER save.
        var pendingAdds = ChangeTracker.Entries<TaskItem>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (pendingHistory.Count == 0 && pendingAdds.Count == 0)
        {
            return result;
        }

        var userId = await _currentUserProvider.GetCurrentUserIdAsync();
        var now = DateTime.UtcNow;

        foreach (var add in pendingAdds)
        {
            TaskHistories.Add(new TaskHistory
            {
                TaskId = add.Id,
                ChangedByUserId = userId,
                ChangedUtc = now,
                ChangeType = TaskChangeType.Created,
            });
        }

        foreach (var h in pendingHistory)
        {
            h.ChangedByUserId = userId;
            h.ChangedUtc = now;
            TaskHistories.Add(h);
        }

        // Persist history rows. This SaveChanges only has TaskHistory adds; recursive audit
        // is a no-op (TaskHistory isn't in the audited type set).
        await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Inspect ChangeTracker for Modified TaskItems and build the history rows the save will
    /// produce. Created/Deleted/Restored markers are emitted instead of raw DeletedUtc rows;
    /// per-field Updated rows cover everything else not in <see cref="ExcludedFieldsFromAudit"/>.
    /// </summary>
    private List<TaskHistory> CapturePendingTaskHistory()
    {
        var rows = new List<TaskHistory>();
        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            // Soft-delete and restore are detected by watching DeletedUtc transitions and
            // emit single marker rows. The DeletedUtc field itself is excluded from the
            // per-field Updated rows below, so we don't double-log.
            var deletedProperty = entry.Property(nameof(TaskItem.DeletedUtc));
            if (deletedProperty.IsModified)
            {
                var was = deletedProperty.OriginalValue as DateTime?;
                var now = deletedProperty.CurrentValue as DateTime?;
                if (was is null && now is not null)
                {
                    rows.Add(new TaskHistory { TaskId = entry.Entity.Id, ChangeType = TaskChangeType.Deleted });
                }
                else if (was is not null && now is null)
                {
                    rows.Add(new TaskHistory { TaskId = entry.Entity.Id, ChangeType = TaskChangeType.Restored });
                }
            }

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified || ExcludedFieldsFromAudit.Contains(prop.Metadata.Name))
                {
                    continue;
                }

                var oldStr = StringifyAuditValue(prop.OriginalValue);
                var newStr = StringifyAuditValue(prop.CurrentValue);
                if (oldStr == newStr)
                {
                    // EF flags a property as modified even when CurrentValue == OriginalValue
                    // if it was set explicitly. Skip these — there's nothing meaningful to log.
                    continue;
                }

                rows.Add(new TaskHistory
                {
                    TaskId = entry.Entity.Id,
                    ChangeType = TaskChangeType.Updated,
                    FieldName = prop.Metadata.Name,
                    OldValue = oldStr,
                    NewValue = newStr,
                });
            }
        }
        return rows;
    }

    private static string? StringifyAuditValue(object? value) => value switch
    {
        null => null,
        DateTime dt => dt.ToString("O"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        TimeOnly t => t.ToString("HH:mm:ss"),
        bool b => b ? "true" : "false",
        _ => value.ToString(),
    };

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new System.Text.StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && !char.IsUpper(name[i + 1]))))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
