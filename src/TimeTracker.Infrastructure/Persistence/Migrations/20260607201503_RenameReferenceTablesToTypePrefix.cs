using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameReferenceTablesToTypePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_calendar_series_tag_attribute_t_time_entry_field_time_entry_field_id",
                table: "t_calendar_series_tag_attribute");

            migrationBuilder.DropForeignKey(
                name: "FK_t_organization_role_t_organization_organization_id",
                table: "t_organization_role");

            migrationBuilder.DropForeignKey(
                name: "FK_t_organization_role_right_t_organization_role_organization_role_id",
                table: "t_organization_role_right");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_attribute_t_time_entry_field_time_entry_field_id",
                table: "t_time_entry_attribute");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_field_t_organization_organization_id",
                table: "t_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_field_t_organization_role_organization_role_id",
                table: "t_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_field_t_type_field_data_type_data_type",
                table: "t_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_field_option_t_time_entry_field_time_entry_field_id",
                table: "t_time_entry_field_option");

            migrationBuilder.DropForeignKey(
                name: "FK_t_user_organization_role_t_organization_role_organization_role_id",
                table: "t_user_organization_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_time_entry_field_option",
                table: "t_time_entry_field_option");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_time_entry_field",
                table: "t_time_entry_field");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_organization_role",
                table: "t_organization_role");

            migrationBuilder.RenameTable(
                name: "t_time_entry_field_option",
                newName: "t_type_time_entry_field_option");

            migrationBuilder.RenameTable(
                name: "t_time_entry_field",
                newName: "t_type_time_entry_field");

            migrationBuilder.RenameTable(
                name: "t_organization_role",
                newName: "t_type_organization_role");

            migrationBuilder.RenameIndex(
                name: "IX_t_time_entry_field_option_time_entry_field_id_sort_order",
                table: "t_type_time_entry_field_option",
                newName: "IX_t_type_time_entry_field_option_time_entry_field_id_sort_order");

            migrationBuilder.RenameIndex(
                name: "IX_t_time_entry_field_organization_role_id",
                table: "t_type_time_entry_field",
                newName: "IX_t_type_time_entry_field_organization_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_t_time_entry_field_organization_id_field_key",
                table: "t_type_time_entry_field",
                newName: "IX_t_type_time_entry_field_organization_id_field_key");

            migrationBuilder.RenameIndex(
                name: "IX_t_time_entry_field_data_type",
                table: "t_type_time_entry_field",
                newName: "IX_t_type_time_entry_field_data_type");

            migrationBuilder.RenameIndex(
                name: "IX_t_organization_role_organization_id_name",
                table: "t_type_organization_role",
                newName: "IX_t_type_organization_role_organization_id_name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_type_time_entry_field_option",
                table: "t_type_time_entry_field_option",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_type_time_entry_field",
                table: "t_type_time_entry_field",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_type_organization_role",
                table: "t_type_organization_role",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_calendar_series_tag_attribute_t_type_time_entry_field_time_entry_field_id",
                table: "t_calendar_series_tag_attribute",
                column: "time_entry_field_id",
                principalTable: "t_type_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_organization_role_right_t_type_organization_role_organization_role_id",
                table: "t_organization_role_right",
                column: "organization_role_id",
                principalTable: "t_type_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_attribute_t_type_time_entry_field_time_entry_field_id",
                table: "t_time_entry_attribute",
                column: "time_entry_field_id",
                principalTable: "t_type_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_type_organization_role_t_organization_organization_id",
                table: "t_type_organization_role",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_type_time_entry_field_t_organization_organization_id",
                table: "t_type_time_entry_field",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_type_time_entry_field_t_type_field_data_type_data_type",
                table: "t_type_time_entry_field",
                column: "data_type",
                principalTable: "t_type_field_data_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_type_time_entry_field_t_type_organization_role_organization_role_id",
                table: "t_type_time_entry_field",
                column: "organization_role_id",
                principalTable: "t_type_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_type_time_entry_field_option_t_type_time_entry_field_time_entry_field_id",
                table: "t_type_time_entry_field_option",
                column: "time_entry_field_id",
                principalTable: "t_type_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_user_organization_role_t_type_organization_role_organization_role_id",
                table: "t_user_organization_role",
                column: "organization_role_id",
                principalTable: "t_type_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_calendar_series_tag_attribute_t_type_time_entry_field_time_entry_field_id",
                table: "t_calendar_series_tag_attribute");

            migrationBuilder.DropForeignKey(
                name: "FK_t_organization_role_right_t_type_organization_role_organization_role_id",
                table: "t_organization_role_right");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_attribute_t_type_time_entry_field_time_entry_field_id",
                table: "t_time_entry_attribute");

            migrationBuilder.DropForeignKey(
                name: "FK_t_type_organization_role_t_organization_organization_id",
                table: "t_type_organization_role");

            migrationBuilder.DropForeignKey(
                name: "FK_t_type_time_entry_field_t_organization_organization_id",
                table: "t_type_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_type_time_entry_field_t_type_field_data_type_data_type",
                table: "t_type_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_type_time_entry_field_t_type_organization_role_organization_role_id",
                table: "t_type_time_entry_field");

            migrationBuilder.DropForeignKey(
                name: "FK_t_type_time_entry_field_option_t_type_time_entry_field_time_entry_field_id",
                table: "t_type_time_entry_field_option");

            migrationBuilder.DropForeignKey(
                name: "FK_t_user_organization_role_t_type_organization_role_organization_role_id",
                table: "t_user_organization_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_type_time_entry_field_option",
                table: "t_type_time_entry_field_option");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_type_time_entry_field",
                table: "t_type_time_entry_field");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_type_organization_role",
                table: "t_type_organization_role");

            migrationBuilder.RenameTable(
                name: "t_type_time_entry_field_option",
                newName: "t_time_entry_field_option");

            migrationBuilder.RenameTable(
                name: "t_type_time_entry_field",
                newName: "t_time_entry_field");

            migrationBuilder.RenameTable(
                name: "t_type_organization_role",
                newName: "t_organization_role");

            migrationBuilder.RenameIndex(
                name: "IX_t_type_time_entry_field_option_time_entry_field_id_sort_order",
                table: "t_time_entry_field_option",
                newName: "IX_t_time_entry_field_option_time_entry_field_id_sort_order");

            migrationBuilder.RenameIndex(
                name: "IX_t_type_time_entry_field_organization_role_id",
                table: "t_time_entry_field",
                newName: "IX_t_time_entry_field_organization_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_t_type_time_entry_field_organization_id_field_key",
                table: "t_time_entry_field",
                newName: "IX_t_time_entry_field_organization_id_field_key");

            migrationBuilder.RenameIndex(
                name: "IX_t_type_time_entry_field_data_type",
                table: "t_time_entry_field",
                newName: "IX_t_time_entry_field_data_type");

            migrationBuilder.RenameIndex(
                name: "IX_t_type_organization_role_organization_id_name",
                table: "t_organization_role",
                newName: "IX_t_organization_role_organization_id_name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_time_entry_field_option",
                table: "t_time_entry_field_option",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_time_entry_field",
                table: "t_time_entry_field",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_organization_role",
                table: "t_organization_role",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_calendar_series_tag_attribute_t_time_entry_field_time_entry_field_id",
                table: "t_calendar_series_tag_attribute",
                column: "time_entry_field_id",
                principalTable: "t_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_organization_role_t_organization_organization_id",
                table: "t_organization_role",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_organization_role_right_t_organization_role_organization_role_id",
                table: "t_organization_role_right",
                column: "organization_role_id",
                principalTable: "t_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_attribute_t_time_entry_field_time_entry_field_id",
                table: "t_time_entry_attribute",
                column: "time_entry_field_id",
                principalTable: "t_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_field_t_organization_organization_id",
                table: "t_time_entry_field",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_field_t_organization_role_organization_role_id",
                table: "t_time_entry_field",
                column: "organization_role_id",
                principalTable: "t_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_field_t_type_field_data_type_data_type",
                table: "t_time_entry_field",
                column: "data_type",
                principalTable: "t_type_field_data_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_field_option_t_time_entry_field_time_entry_field_id",
                table: "t_time_entry_field_option",
                column: "time_entry_field_id",
                principalTable: "t_time_entry_field",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_t_user_organization_role_t_organization_role_organization_role_id",
                table: "t_user_organization_role",
                column: "organization_role_id",
                principalTable: "t_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
