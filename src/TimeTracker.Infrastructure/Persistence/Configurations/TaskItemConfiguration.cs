using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("t_task");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Project).HasMaxLength(200);
        builder.Property(x => x.ReferenceCode).HasMaxLength(40);
        builder.Property(x => x.ExternalUrl).HasMaxLength(500);
        builder.Property(x => x.EstimatedHours).HasColumnType("decimal(7,2)");
        builder.Property(x => x.PercentComplete).HasDefaultValue(0);
        builder.Property(x => x.DueDate).HasColumnType("date");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on the assignee FK to avoid multiple cascade paths into t_task (creator→Cascade
        // already exists). Removing a user with assigned tasks requires explicit cleanup.
        builder.HasOne(x => x.AssignedTo)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AssignedToUserId, x.IsComplete });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ProjectEntity)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.IsComplete });
    }
}
