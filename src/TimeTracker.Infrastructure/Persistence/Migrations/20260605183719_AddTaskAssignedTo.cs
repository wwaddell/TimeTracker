using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignedTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "assigned_to_user_id",
                table: "t_task",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: existing tasks are assigned to their creator. Must run before the FK
            // is created (otherwise rows with assigned_to_user_id=0 would violate it).
            migrationBuilder.Sql("UPDATE t_task SET assigned_to_user_id = user_id;");

            migrationBuilder.CreateIndex(
                name: "IX_t_task_assigned_to_user_id_is_complete",
                table: "t_task",
                columns: new[] { "assigned_to_user_id", "is_complete" });

            migrationBuilder.AddForeignKey(
                name: "FK_t_task_t_user_assigned_to_user_id",
                table: "t_task",
                column: "assigned_to_user_id",
                principalTable: "t_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_task_t_user_assigned_to_user_id",
                table: "t_task");

            migrationBuilder.DropIndex(
                name: "IX_t_task_assigned_to_user_id_is_complete",
                table: "t_task");

            migrationBuilder.DropColumn(
                name: "assigned_to_user_id",
                table: "t_task");
        }
    }
}
