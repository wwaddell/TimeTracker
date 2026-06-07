using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryContextCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "capture_location",
                table: "t_user",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "week_starts_on",
                table: "t_user",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "t_time_entry",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "location_accuracy",
                table: "t_time_entry",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "t_time_entry",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "t_time_entry",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capture_location",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "week_starts_on",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "location_accuracy",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "t_time_entry");
        }
    }
}
