using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("t_user");
        builder.HasKey(x => x.Id);
        // Nullable: invited users have no external id until first login.
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[external_id] IS NOT NULL");
        builder.HasIndex(x => x.Email);
    }
}
