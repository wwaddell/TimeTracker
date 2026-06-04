using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api;

/// <summary>
/// Development-only bootstrap: applies migrations and seeds a stand-in user, a couple
/// of organizations with roles, and sample configurable fields. This stands in for
/// real authentication (Entra External ID) until that is wired up.
/// </summary>
public static class DevData
{
    /// <summary>Stable external id used for the local dev user until auth exists.</summary>
    public const string DevUserExternalId = "dev-local-user";

    public static async Task EnsureSeededAsync(TimeTrackerDbContext db)
    {
        await db.Database.MigrateAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == DevUserExternalId);
        if (user is null)
        {
            user = new User
            {
                ExternalId = DevUserExternalId,
                Email = "dev@local",
                DisplayName = "Dev User",
                CreatedUtc = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        if (await db.Organizations.AnyAsync())
        {
            return; // already seeded
        }

        // --- Acme Consulting: a richer, billable-time configuration ---
        var acme = new Organization { Name = "Acme Consulting", Description = "Consulting services org.", CreatedUtc = DateTime.UtcNow };
        var acmeAdmin = new OrganizationRole { Organization = acme, Name = "Administrator", SortOrder = 1, CreatedUtc = DateTime.UtcNow };
        GrantAll(acmeAdmin);
        var consultant = new OrganizationRole { Organization = acme, Name = "Consultant", SortOrder = 2, CreatedUtc = DateTime.UtcNow };
        Grant(consultant, OrgRight.ManageFields);
        acme.Roles.Add(acmeAdmin);
        acme.Roles.Add(consultant);
        acme.TimeEntryFields.Add(new TimeEntryField
        {
            FieldKey = "project", Label = "Project", DataType = FieldDataType.Text,
            IsRequired = true, SortOrder = 1, CreatedUtc = DateTime.UtcNow,
        });
        acme.TimeEntryFields.Add(new TimeEntryField
        {
            FieldKey = "billable", Label = "Billable", DataType = FieldDataType.Boolean,
            IsRequired = false, SortOrder = 2, CreatedUtc = DateTime.UtcNow,
        });
        var category = new TimeEntryField
        {
            FieldKey = "category", Label = "Category", DataType = FieldDataType.Select,
            IsRequired = false, SortOrder = 3, CreatedUtc = DateTime.UtcNow,
        };
        category.Options.Add(new TimeEntryFieldOption { Value = "meeting", Label = "Meeting", SortOrder = 1 });
        category.Options.Add(new TimeEntryFieldOption { Value = "development", Label = "Development", SortOrder = 2 });
        category.Options.Add(new TimeEntryFieldOption { Value = "support", Label = "Support", SortOrder = 3 });
        acme.TimeEntryFields.Add(category);

        // --- Personal: minimal configuration (just the base note) ---
        var personal = new Organization { Name = "Personal", CreatedUtc = DateTime.UtcNow };
        var owner = new OrganizationRole { Organization = personal, Name = "Owner", SortOrder = 1, CreatedUtc = DateTime.UtcNow };
        GrantAll(owner);
        personal.Roles.Add(owner);

        db.Organizations.AddRange(acme, personal);
        await db.SaveChangesAsync();

        // Memberships + role assignments (dev user is Administrator + Consultant at Acme, Owner at Personal).
        var acmeMembership = new UserOrganization
        {
            UserId = user.Id, OrganizationId = acme.Id, IsDefault = true, CreatedUtc = DateTime.UtcNow,
        };
        var personalMembership = new UserOrganization
        {
            UserId = user.Id, OrganizationId = personal.Id, IsDefault = false, CreatedUtc = DateTime.UtcNow,
        };
        db.UserOrganizations.AddRange(acmeMembership, personalMembership);
        await db.SaveChangesAsync();

        db.UserOrganizationRoles.AddRange(
            new UserOrganizationRole { UserOrganizationId = acmeMembership.Id, OrganizationRoleId = acmeAdmin.Id },
            new UserOrganizationRole { UserOrganizationId = acmeMembership.Id, OrganizationRoleId = consultant.Id },
            new UserOrganizationRole { UserOrganizationId = personalMembership.Id, OrganizationRoleId = owner.Id });
        await db.SaveChangesAsync();
    }

    private static void GrantAll(OrganizationRole role)
    {
        foreach (var right in Enum.GetValues<OrgRight>())
        {
            role.Rights.Add(new OrganizationRoleRight { Right = right });
        }
    }

    private static void Grant(OrganizationRole role, params OrgRight[] rights)
    {
        foreach (var right in rights)
        {
            role.Rights.Add(new OrganizationRoleRight { Right = right });
        }
    }
}
