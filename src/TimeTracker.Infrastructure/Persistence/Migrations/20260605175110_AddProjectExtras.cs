using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_url",
                table: "t_project",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_code",
                table: "t_project",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.InsertData(
                table: "t_type_right",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[] { 6, "manage_projects", "Create and edit organization projects.", "Manage projects" });

            migrationBuilder.CreateIndex(
                name: "IX_t_project_organization_id_reference_code",
                table: "t_project",
                columns: new[] { "organization_id", "reference_code" },
                unique: true,
                filter: "[deleted_utc] IS NULL AND [reference_code] IS NOT NULL");

            // Backfill: any role that already had ManageOrganization (right=1) is also granted
            // ManageProjects (right=6). Idempotent — NOT EXISTS guards against re-runs.
            migrationBuilder.Sql(@"
                INSERT INTO t_organization_role_right (organization_role_id, [right])
                SELECT r.organization_role_id, 6
                FROM t_organization_role_right r
                WHERE r.[right] = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM t_organization_role_right x
                      WHERE x.organization_role_id = r.organization_role_id AND x.[right] = 6);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_t_project_organization_id_reference_code",
                table: "t_project");

            migrationBuilder.DeleteData(
                table: "t_type_right",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "external_url",
                table: "t_project");

            migrationBuilder.DropColumn(
                name: "reference_code",
                table: "t_project");
        }
    }
}
