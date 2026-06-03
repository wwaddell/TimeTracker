using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TimeEntryFieldConfiguration : IEntityTypeConfiguration<TimeEntryField>
{
    public void Configure(EntityTypeBuilder<TimeEntryField> builder)
    {
        builder.ToTable("t_time_entry_field");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FieldKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.TimeEntryFields)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OrganizationRole)
            .WithMany()
            .HasForeignKey(x => x.OrganizationRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // The enum value doubles as the FK into t_type_field_data_type.
        builder.HasOne(x => x.DataTypeLookup)
            .WithMany()
            .HasForeignKey(x => x.DataType)
            .OnDelete(DeleteBehavior.Restrict);

        // Field keys are unique within an organization.
        builder.HasIndex(x => new { x.OrganizationId, x.FieldKey }).IsUnique();
    }
}
