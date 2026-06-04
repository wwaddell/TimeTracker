using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Admin;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Authenticated; each endpoint then checks the specific org right it needs.
        var admin = app.MapGroup("").RequireAuthorization();

        // Roles defined by the org (id+name) — any member may read, used to scope fields/entries.
        admin.MapGet("/api/organizations/{orgId:int}/roles", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await IsMemberAsync(db, currentUser, orgId))
            {
                return Results.Forbid();
            }

            var roles = await db.OrganizationRoles
                .Where(r => r.OrganizationId == orgId)
                .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                .Select(r => new OrganizationRoleDto(r.Id, r.Name))
                .ToListAsync();

            return Results.Ok(roles);
        });

        // All configurable fields for the org (including inactive, all roles).
        admin.MapGet("/api/organizations/{orgId:int}/admin/entry-fields", async (int orgId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageFields))
            {
                return Results.Forbid();
            }

            var fields = await db.TimeEntryFields
                .Where(f => f.OrganizationId == orgId)
                .OrderBy(f => f.SortOrder).ThenBy(f => f.Label)
                .Select(f => new EntryFieldAdminDto(
                    f.Id, f.FieldKey, f.Label, f.DataType, f.IsRequired, f.SortOrder, f.IsActive,
                    f.OrganizationRoleId,
                    f.OrganizationRole != null ? f.OrganizationRole.Name : null,
                    f.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new EntryFieldOptionInput(o.Value, o.Label, o.SortOrder)).ToList()))
                .ToListAsync();

            return Results.Ok(fields);
        });

        // Create a field.
        admin.MapPost("/api/organizations/{orgId:int}/admin/entry-fields",
            async (int orgId, SaveEntryFieldRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageFields))
            {
                return Results.Forbid();
            }

            var validation = await ValidateAsync(db, orgId, req, fieldId: null);
            if (validation is not null)
            {
                return validation;
            }

            var field = new TimeEntryField
            {
                OrganizationId = orgId,
                OrganizationRoleId = req.OrganizationRoleId,
                FieldKey = req.FieldKey.Trim(),
                Label = req.Label.Trim(),
                DataType = req.DataType,
                IsRequired = req.IsRequired,
                SortOrder = req.SortOrder,
                IsActive = req.IsActive,
                CreatedUtc = DateTime.UtcNow,
            };
            ApplyOptions(field, req);

            db.TimeEntryFields.Add(field);
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizations/{orgId}/admin/entry-fields/{field.Id}", new { field.Id });
        });

        // Update a field (replaces its option set).
        admin.MapPut("/api/organizations/{orgId:int}/admin/entry-fields/{fieldId:int}",
            async (int orgId, int fieldId, SaveEntryFieldRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageFields))
            {
                return Results.Forbid();
            }

            var field = await db.TimeEntryFields
                .Include(f => f.Options)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.OrganizationId == orgId);
            if (field is null)
            {
                return Results.NotFound();
            }

            var validation = await ValidateAsync(db, orgId, req, fieldId);
            if (validation is not null)
            {
                return validation;
            }

            field.OrganizationRoleId = req.OrganizationRoleId;
            field.FieldKey = req.FieldKey.Trim();
            field.Label = req.Label.Trim();
            field.DataType = req.DataType;
            field.IsRequired = req.IsRequired;
            field.SortOrder = req.SortOrder;
            field.IsActive = req.IsActive;
            field.ModifiedUtc = DateTime.UtcNow;

            db.TimeEntryFieldOptions.RemoveRange(field.Options);
            field.Options.Clear();
            ApplyOptions(field, req);

            await db.SaveChangesAsync();
            return Results.Ok(new { field.Id });
        });

        // Delete a field — or deactivate it if it already has logged values (FK is Restrict).
        admin.MapDelete("/api/organizations/{orgId:int}/admin/entry-fields/{fieldId:int}",
            async (int orgId, int fieldId, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageFields))
            {
                return Results.Forbid();
            }

            var field = await db.TimeEntryFields
                .Include(f => f.Options)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.OrganizationId == orgId);
            if (field is null)
            {
                return Results.NotFound();
            }

            var inUse = await db.TimeEntryAttributes.AnyAsync(a => a.TimeEntryFieldId == fieldId);
            if (inUse)
            {
                field.IsActive = false;
                field.ModifiedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(new DeleteFieldResult(false, true,
                    "Field has logged values, so it was deactivated (hidden from new entries) rather than deleted."));
            }

            db.TimeEntryFieldOptions.RemoveRange(field.Options);
            db.TimeEntryFields.Remove(field);
            await db.SaveChangesAsync();
            return Results.Ok(new DeleteFieldResult(true, false, "Field deleted."));
        });
    }

    private static async Task<bool> IsMemberAsync(TimeTrackerDbContext db, ICurrentUser currentUser, int orgId)
    {
        var userId = await currentUser.GetUserIdAsync();
        return await db.UserOrganizations.AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);
    }

    private static async Task<IResult?> ValidateAsync(
        TimeTrackerDbContext db, int orgId, SaveEntryFieldRequest req, int? fieldId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(req.Label))
        {
            errors["label"] = ["Label is required."];
        }

        var key = req.FieldKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            errors["fieldKey"] = ["Field key is required."];
        }
        else if (await db.TimeEntryFields.AnyAsync(f =>
                     f.OrganizationId == orgId && f.FieldKey == key && f.Id != fieldId))
        {
            errors["fieldKey"] = ["A field with this key already exists in this organization."];
        }

        if (req.OrganizationRoleId is { } roleId &&
            !await db.OrganizationRoles.AnyAsync(r => r.Id == roleId && r.OrganizationId == orgId))
        {
            errors["organizationRoleId"] = ["The selected role does not belong to this organization."];
        }

        if (req.DataType == FieldDataType.Select && req.Options.Count == 0)
        {
            errors["options"] = ["A select field needs at least one option."];
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static void ApplyOptions(TimeEntryField field, SaveEntryFieldRequest req)
    {
        if (req.DataType != FieldDataType.Select)
        {
            return;
        }

        var sort = 0;
        foreach (var opt in req.Options)
        {
            if (string.IsNullOrWhiteSpace(opt.Value))
            {
                continue;
            }

            field.Options.Add(new TimeEntryFieldOption
            {
                Value = opt.Value.Trim(),
                Label = string.IsNullOrWhiteSpace(opt.Label) ? opt.Value.Trim() : opt.Label.Trim(),
                SortOrder = opt.SortOrder == 0 ? ++sort : opt.SortOrder,
            });
        }
    }
}
