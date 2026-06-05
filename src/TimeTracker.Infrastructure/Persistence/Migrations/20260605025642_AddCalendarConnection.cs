using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_calendar_connection",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    account_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    tenant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    refresh_token_protected = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    last_sync_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_calendar_connection", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_calendar_connection_t_user_user_id",
                        column: x => x.user_id,
                        principalTable: "t_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_connection_user_id_provider",
                table: "t_calendar_connection",
                columns: new[] { "user_id", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_calendar_connection");
        }
    }
}
