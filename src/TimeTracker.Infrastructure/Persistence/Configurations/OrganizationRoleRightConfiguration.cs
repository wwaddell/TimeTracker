using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class OrganizationRoleRightConfiguration : IEntityTypeConfiguration<OrganizationRoleRight>
{
    public void Configure(EntityTypeBuilder<OrganizationRoleRight> builder)
    {
        builder.ToTable("t_organization_role_right");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.OrganizationRole)
            .WithMany(r => r.Rights)
            .HasForeignKey(x => x.OrganizationRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // The enum value doubles as the FK into t_type_right.
        builder.HasOne(x => x.RightLookup)
            .WithMany()
            .HasForeignKey(x => x.Right)
            .OnDelete(DeleteBehavior.Restrict);

        // A role grants each right at most once.
        builder.HasIndex(x => new { x.OrganizationRoleId, x.Right }).IsUnique();
    }
}
