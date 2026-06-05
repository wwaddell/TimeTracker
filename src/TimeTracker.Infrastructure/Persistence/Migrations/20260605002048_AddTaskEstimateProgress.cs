using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEstimateProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "estimated_hours",
                table: "t_task",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "percent_complete",
                table: "t_task",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "estimated_hours",
                table: "t_task");

            migrationBuilder.DropColumn(
                name: "percent_complete",
                table: "t_task");
        }
    }
}
