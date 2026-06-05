using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class CalendarSeriesTagConfiguration : IEntityTypeConfiguration<CalendarSeriesTag>
{
    public void Configure(EntityTypeBuilder<CalendarSeriesTag> builder)
    {
        builder.ToTable("t_calendar_series_tag");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeriesUid).IsRequired().HasMaxLength(512);

        // Restrict (not cascade) to avoid a second cascade path into this table
        // (User → Task → series_tag already exists via SetNull). Matches t_time_entry.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Task)
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        // One remembered tag per series per user per org.
        builder.HasIndex(x => new { x.OrganizationId, x.UserId, x.SeriesUid }).IsUnique();
    }
}
