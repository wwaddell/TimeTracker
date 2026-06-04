using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class RightLookupConfiguration : IEntityTypeConfiguration<RightLookup>
{
    public void Configure(EntityTypeBuilder<RightLookup> builder)
    {
        builder.ToTable("t_type_right");

        // Ids are fixed to the OrgRight enum values, so don't auto-generate.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(300);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new RightLookup { Id = OrgRight.ManageOrganization, Code = "manage_organization", Name = "Manage organization", Description = "Edit organization details." },
            new RightLookup { Id = OrgRight.ManageUsers, Code = "manage_users", Name = "Manage users", Description = "Invite members and assign their roles." },
            new RightLookup { Id = OrgRight.ManageRoles, Code = "manage_roles", Name = "Manage roles", Description = "Create and edit roles and their rights." },
            new RightLookup { Id = OrgRight.ManageFields, Code = "manage_fields", Name = "Manage fields", Description = "Configure the time-entry fields." },
            new RightLookup { Id = OrgRight.ViewReports, Code = "view_reports", Name = "View reports", Description = "View time reports and summaries." });
    }
}
