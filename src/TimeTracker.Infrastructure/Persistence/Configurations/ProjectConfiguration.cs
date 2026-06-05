using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("t_project");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.ReferenceCode).HasMaxLength(40);
        builder.Property(x => x.ExternalUrl).HasMaxLength(500);

        // Reference codes are unique within an org (and case-sensitive at the DB level — we
        // compare case-insensitively at the app layer). Filtered to non-deleted rows with a code.
        builder.HasIndex(x => new { x.OrganizationId, x.ReferenceCode })
            .IsUnique()
            .HasFilter("[deleted_utc] IS NULL AND [reference_code] IS NOT NULL");

        // Restrict (not cascade) so the TaskItem/TimeEntry → Project SetNull paths don't
        // collide with their existing Organization paths (SQL Server forbids multiple cascade
        // paths through the same table). Orgs aren't hard-deleted in practice (they use IsActive).
        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project names are unique within an org. Filtered so soft-deleted rows don't conflict.
        builder.HasIndex(x => new { x.OrganizationId, x.Name })
            .IsUnique()
            .HasFilter("[deleted_utc] IS NULL");
    }
}
