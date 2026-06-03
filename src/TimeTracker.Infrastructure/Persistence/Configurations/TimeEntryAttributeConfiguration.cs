using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TimeEntryAttributeConfiguration : IEntityTypeConfiguration<TimeEntryAttribute>
{
    public void Configure(EntityTypeBuilder<TimeEntryAttribute> builder)
    {
        builder.ToTable("t_time_entry_attribute");
        builder.HasKey(x => x.Id);

        // Raw value interpreted per the field's data type; long values allowed.
        builder.Property(x => x.Value);

        builder.HasOne(x => x.TimeEntry)
            .WithMany(e => e.Attributes)
            .HasForeignKey(x => x.TimeEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict so deleting a field definition doesn't silently drop entry data.
        builder.HasOne(x => x.TimeEntryField)
            .WithMany()
            .HasForeignKey(x => x.TimeEntryFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        // One value per field per entry.
        builder.HasIndex(x => new { x.TimeEntryId, x.TimeEntryFieldId }).IsUnique();
    }
}
