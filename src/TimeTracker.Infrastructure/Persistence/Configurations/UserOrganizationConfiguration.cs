using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class UserOrganizationConfiguration : IEntityTypeConfiguration<UserOrganization>
{
    public void Configure(EntityTypeBuilder<UserOrganization> builder)
    {
        builder.ToTable("t_user_organization");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithMany(u => u.Organizations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on org side to avoid multiple cascade paths into this table.
        builder.HasOne(x => x.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // A user belongs to a given org at most once.
        builder.HasIndex(x => new { x.UserId, x.OrganizationId }).IsUnique();
    }
}
