using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePrimaryKeyColumnsToTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_user_organization_role",
                newName: "user_organization_role_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_user_organization",
                newName: "user_organization_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_user",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_type_time_entry_field_option",
                newName: "time_entry_field_option_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_type_time_entry_field",
                newName: "time_entry_field_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_type_right",
                newName: "right_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_type_organization_role",
                newName: "organization_role_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_type_field_data_type",
                newName: "field_data_type_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_time_entry_attribute",
                newName: "time_entry_attribute_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_time_entry",
                newName: "time_entry_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_task",
                newName: "task_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_project_member",
                newName: "project_member_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_project",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_organization_role_right",
                newName: "organization_role_right_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_organization",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_calendar_series_tag_attribute",
                newName: "calendar_series_tag_attribute_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_calendar_series_tag",
                newName: "calendar_series_tag_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "t_calendar_connection",
                newName: "calendar_connection_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "user_organization_role_id",
                table: "t_user_organization_role",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "user_organization_id",
                table: "t_user_organization",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "t_user",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "time_entry_field_option_id",
                table: "t_type_time_entry_field_option",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "time_entry_field_id",
                table: "t_type_time_entry_field",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "right_id",
                table: "t_type_right",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "organization_role_id",
                table: "t_type_organization_role",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "field_data_type_id",
                table: "t_type_field_data_type",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "time_entry_attribute_id",
                table: "t_time_entry_attribute",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "time_entry_id",
                table: "t_time_entry",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "task_id",
                table: "t_task",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "project_member_id",
                table: "t_project_member",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "project_id",
                table: "t_project",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "organization_role_right_id",
                table: "t_organization_role_right",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "t_organization",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "calendar_series_tag_attribute_id",
                table: "t_calendar_series_tag_attribute",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "calendar_series_tag_id",
                table: "t_calendar_series_tag",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "calendar_connection_id",
                table: "t_calendar_connection",
                newName: "id");
        }
    }
}
