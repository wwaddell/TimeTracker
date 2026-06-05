using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class CalendarConnectionConfiguration : IEntityTypeConfiguration<CalendarConnection>
{
    public void Configure(EntityTypeBuilder<CalendarConnection> builder)
    {
        builder.ToTable("t_calendar_connection");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.AccountEmail).HasMaxLength(320);
        builder.Property(x => x.TenantId).HasMaxLength(100);
        builder.Property(x => x.RefreshTokenProtected).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One linked account per provider per user.
        builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
    }
}
