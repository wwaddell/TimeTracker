using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_task_history",
                columns: table => new
                {
                    task_history_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    task_id = table.Column<int>(type: "int", nullable: false),
                    changed_by_user_id = table.Column<int>(type: "int", nullable: true),
                    changed_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    change_type = table.Column<byte>(type: "tinyint", nullable: false),
                    field_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    old_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_task_history", x => x.task_history_id);
                    table.ForeignKey(
                        name: "FK_t_task_history_t_task_task_id",
                        column: x => x.task_id,
                        principalTable: "t_task",
                        principalColumn: "task_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_task_history_t_user_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "t_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_task_history_changed_by_user_id",
                table: "t_task_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_task_history_task_id_changed_utc",
                table: "t_task_history",
                columns: new[] { "task_id", "changed_utc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_task_history");
        }
    }
}
