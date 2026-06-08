using Microsoft.EntityFrameworkCore;
using TimeTracker.Api.Auth;
using TimeTracker.Contracts.Admin;
using TimeTracker.Contracts.Organizations;
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

        // Entry-form settings (e.g. whether a start time is captured). ManageFields right.
        admin.MapPut("/api/organizations/{orgId:int}/entry-settings",
            async (int orgId, EntrySettingsRequest req, TimeTrackerDbContext db, ICurrentUser currentUser) =>
        {
            if (!await currentUser.HasRightAsync(orgId, OrgRight.ManageFields))
            {
                return Results.Forbid();
            }

            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org is null)
            {
                return Results.NotFound();
            }

            org.RequireTime = req.RequireTime;
            org.CaptureLocation = req.CaptureLocation;
            org.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { org.Id, org.RequireTime, org.CaptureLocation });
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
                    f.DefaultValue,
                    f.IsSystem,
                    f.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new EntryFieldOptionInput(o.Value, o.Label, o.SortOrder, o.Icon)).ToList()))
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
                DefaultValue = NormalizeDefaultValue(req),
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

            // System fields lock down structural properties: only IsRequired / IsActive /
            // SortOrder / OrganizationRoleId can change. Skip the full validation in that
            // case so a client sending the original (unchanged) Label/Key/Type doesn't hit
            // the FieldKey-uniqueness check against itself.
            if (field.IsSystem)
            {
                field.IsRequired = req.IsRequired;
                field.SortOrder = req.SortOrder;
                field.IsActive = req.IsActive;
                if (req.OrganizationRoleId is { } roleId &&
                    !await db.OrganizationRoles.AnyAsync(r => r.Id == roleId && r.OrganizationId == orgId))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["organizationRoleId"] = ["The selected role does not belong to this organization."],
                    });
                }
                field.OrganizationRoleId = req.OrganizationRoleId;
                field.ModifiedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(new { field.Id });
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
            field.DefaultValue = NormalizeDefaultValue(req);
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

            // System fields can be turned off but never destroyed. Toggling IsActive=false
            // hides them from new entries, which is the natural "delete" semantic here.
            if (field.IsSystem)
            {
                if (field.IsActive)
                {
                    field.IsActive = false;
                    field.ModifiedUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                return Results.Ok(new DeleteFieldResult(false, true,
                    "System fields can't be deleted; this one was deactivated instead."));
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

        // Default-value validation — interpretation depends on DataType.
        var def = req.DefaultValue?.Trim();
        if (!string.IsNullOrEmpty(def))
        {
            if (def.Length > 200)
            {
                errors["defaultValue"] = ["Default value must be 200 characters or fewer."];
            }
            else
            {
                switch (req.DataType)
                {
                    case FieldDataType.Boolean:
                        if (!string.Equals(def, "true", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(def, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            errors["defaultValue"] = ["Boolean default must be true, false, or empty (no default)."];
                        }
                        break;
                    case FieldDataType.Number:
                        if (!decimal.TryParse(def, System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                        {
                            errors["defaultValue"] = ["Number default must be a valid number."];
                        }
                        break;
                    case FieldDataType.Select:
                        var validValues = req.Options
                            .Where(o => !string.IsNullOrWhiteSpace(o.Value))
                            .Select(o => o.Value)
                            .ToHashSet(StringComparer.Ordinal);
                        if (!validValues.Contains(def))
                        {
                            errors["defaultValue"] = ["Select default must match one of the defined option values."];
                        }
                        break;
                        // Date and Text accept anything reasonable; client picker enforces format.
                }
            }
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    // Empty / whitespace ⇒ null (no default). Booleans are lowercased so the stored form is
    // canonical regardless of how the client wrote it.
    private static string? NormalizeDefaultValue(SaveEntryFieldRequest req)
    {
        var def = req.DefaultValue?.Trim();
        if (string.IsNullOrEmpty(def))
        {
            return null;
        }
        if (req.DataType == FieldDataType.Boolean)
        {
            return string.Equals(def, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        }
        return def;
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
                Icon = string.IsNullOrWhiteSpace(opt.Icon) ? null : opt.Icon.Trim(),
                SortOrder = opt.SortOrder == 0 ? ++sort : opt.SortOrder,
            });
        }
    }
}
