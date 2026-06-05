using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TimeEntryFieldOptionConfiguration : IEntityTypeConfiguration<TimeEntryFieldOption>
{
    public void Configure(EntityTypeBuilder<TimeEntryFieldOption> builder)
    {
        builder.ToTable("t_time_entry_field_option");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Icon).HasMaxLength(64);

        builder.HasOne(x => x.TimeEntryField)
            .WithMany(f => f.Options)
            .HasForeignKey(x => x.TimeEntryFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TimeEntryFieldId, x.SortOrder });
    }
}
