using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class TaskHistoryConfiguration : IEntityTypeConfiguration<TaskHistory>
{
    public void Configure(EntityTypeBuilder<TaskHistory> builder)
    {
        builder.ToTable("t_task_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName).HasMaxLength(100);
        // OldValue/NewValue stay unbounded — task fields include Description and ExternalUrl
        // which can be long. SQL Server picks nvarchar(max) by default for unbounded strings.

        builder.HasOne(x => x.Task)
            .WithMany() // no nav property on TaskItem; history is loaded via a dedicated query
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict); // tasks soft-delete; history outlives them

        builder.HasOne(x => x.ChangedBy)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Primary query path: pull a task's history newest-first.
        builder.HasIndex(x => new { x.TaskId, x.ChangedUtc }).IsDescending(false, true);
    }
}
