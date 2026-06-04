using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class UserOrganizationRoleConfiguration : IEntityTypeConfiguration<UserOrganizationRole>
{
    public void Configure(EntityTypeBuilder<UserOrganizationRole> builder)
    {
        builder.ToTable("t_user_organization_role");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.UserOrganization)
            .WithMany(m => m.Roles)
            .HasForeignKey(x => x.UserOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict so a role can't be deleted while still assigned to members.
        builder.HasOne(x => x.OrganizationRole)
            .WithMany(r => r.Members)
            .HasForeignKey(x => x.OrganizationRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // A membership holds each role at most once.
        builder.HasIndex(x => new { x.UserOrganizationId, x.OrganizationRoleId }).IsUnique();
    }
}
