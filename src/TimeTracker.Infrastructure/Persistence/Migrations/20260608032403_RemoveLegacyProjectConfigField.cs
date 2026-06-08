using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyProjectConfigField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the seeded "project" configurable field — superseded by t_project.
            // Three Restrict FKs reference the field row, so clean them out first:
            //   1. t_time_entry_attribute — legacy per-entry values
            //   2. t_calendar_series_tag_attribute — calendar-import series tag values
            //   3. t_type_time_entry_field_option — the Select options
            // Then drop the field row. No-op if a fresh DB never had it.
            migrationBuilder.Sql(@"
                DECLARE @projectFieldIds TABLE (id INT);
                INSERT INTO @projectFieldIds (id)
                SELECT time_entry_field_id FROM t_type_time_entry_field WHERE field_key = 'project';

                DELETE FROM t_time_entry_attribute
                WHERE  time_entry_field_id IN (SELECT id FROM @projectFieldIds);

                DELETE FROM t_calendar_series_tag_attribute
                WHERE  time_entry_field_id IN (SELECT id FROM @projectFieldIds);

                DELETE FROM t_type_time_entry_field_option
                WHERE  time_entry_field_id IN (SELECT id FROM @projectFieldIds);

                DELETE FROM t_type_time_entry_field
                WHERE  time_entry_field_id IN (SELECT id FROM @projectFieldIds);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-create only the field row (orphan attribute data is gone — can't reconstruct).
            // Mirrors the original DevData seed: org 1, Select type (5), system, active.
            migrationBuilder.Sql(@"
                INSERT INTO t_type_time_entry_field
                    (organization_id, field_key, label, data_type, is_required, sort_order,
                     is_active, is_system, created_utc)
                VALUES
                    (1, 'project', 'Project', 5, 1, 1, 1, 1, SYSUTCDATETIME());");
        }
    }
}
