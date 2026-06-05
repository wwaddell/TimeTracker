using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("t_time_entry");
        builder.HasKey(x => x.Id);

        // The always-required base field; allow long free text.
        builder.Property(x => x.Note).IsRequired();
        builder.Property(x => x.EntryDate).HasColumnType("date");
        builder.Property(x => x.StartTime).HasColumnType("time");

        // Import provenance. Stored so the same calendar occurrence can never be imported twice.
        builder.Property(x => x.Source).HasDefaultValue(Domain.Enums.TimeEntrySource.Manual);
        builder.Property(x => x.SourceSeriesUid).HasMaxLength(512);
        builder.Property(x => x.SourceEventId).HasMaxLength(512);

        // Restrict user/org deletes so they don't form multiple cascade paths
        // into t_time_entry (and onward to t_time_entry_attribute).
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Task)
            .WithMany(t => t.TimeEntries)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.EntryDate });
        builder.HasIndex(x => new { x.OrganizationId, x.EntryDate });

        // Enforce idempotent imports: a given calendar occurrence can be logged at most
        // once per user per org. Filtered to imported, non-deleted rows — so soft-deleting an
        // imported entry frees it to be imported again.
        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.SourceSeriesUid, x.SourceOccurrenceStartUtc })
            .IsUnique()
            .HasFilter("[source_series_uid] IS NOT NULL AND [deleted_utc] IS NULL");
    }
}
