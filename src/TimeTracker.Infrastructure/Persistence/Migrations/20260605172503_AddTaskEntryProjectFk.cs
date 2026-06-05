using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEntryProjectFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_project_t_organization_organization_id",
                table: "t_project");

            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "t_time_entry",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "t_task",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_project_id",
                table: "t_time_entry",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_task_project_id",
                table: "t_task",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_project_t_organization_organization_id",
                table: "t_project",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_task_t_project_project_id",
                table: "t_task",
                column: "project_id",
                principalTable: "t_project",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_t_time_entry_t_project_project_id",
                table: "t_time_entry",
                column: "project_id",
                principalTable: "t_project",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_project_t_organization_organization_id",
                table: "t_project");

            migrationBuilder.DropForeignKey(
                name: "FK_t_task_t_project_project_id",
                table: "t_task");

            migrationBuilder.DropForeignKey(
                name: "FK_t_time_entry_t_project_project_id",
                table: "t_time_entry");

            migrationBuilder.DropIndex(
                name: "IX_t_time_entry_project_id",
                table: "t_time_entry");

            migrationBuilder.DropIndex(
                name: "IX_t_task_project_id",
                table: "t_task");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "t_task");

            migrationBuilder.AddForeignKey(
                name: "FK_t_project_t_organization_organization_id",
                table: "t_project",
                column: "organization_id",
                principalTable: "t_organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
