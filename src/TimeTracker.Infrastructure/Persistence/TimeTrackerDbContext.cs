using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Time Tracker database. Entity-to-table mapping
/// (<c>t_*</c> / <c>t_type_*</c>) lives in the per-entity configuration classes
/// in <c>Persistence/Configurations</c>. Column names are converted to snake_case
/// by a global convention below.
/// </summary>
public class TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeTrackerDbContext).Assembly);

        // Map every column to snake_case based on its CLR property name so the
        // schema matches the house naming style (note, entry_date, organization_id, ...).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

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
