using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCaptureLocationToOrg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capture_location",
                table: "t_user");

            migrationBuilder.AddColumn<bool>(
                name: "capture_location",
                table: "t_organization",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capture_location",
                table: "t_organization");

            migrationBuilder.AddColumn<bool>(
                name: "capture_location",
                table: "t_user",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
