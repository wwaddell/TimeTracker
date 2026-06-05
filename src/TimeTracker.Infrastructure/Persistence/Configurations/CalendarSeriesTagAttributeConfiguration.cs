using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class CalendarSeriesTagAttributeConfiguration : IEntityTypeConfiguration<CalendarSeriesTagAttribute>
{
    public void Configure(EntityTypeBuilder<CalendarSeriesTagAttribute> builder)
    {
        builder.ToTable("t_calendar_series_tag_attribute");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value);

        builder.HasOne(x => x.CalendarSeriesTag)
            .WithMany(t => t.Attributes)
            .HasForeignKey(x => x.CalendarSeriesTagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict so deleting a field definition doesn't silently drop remembered tags.
        builder.HasOne(x => x.TimeEntryField)
            .WithMany()
            .HasForeignKey(x => x.TimeEntryFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CalendarSeriesTagId, x.TimeEntryFieldId }).IsUnique();
    }
}
