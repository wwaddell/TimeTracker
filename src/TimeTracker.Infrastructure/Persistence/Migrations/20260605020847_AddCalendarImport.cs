using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "t_time_entry",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "source_event_id",
                table: "t_time_entry",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "source_occurrence_start_utc",
                table: "t_time_entry",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_series_uid",
                table: "t_time_entry",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "t_calendar_series_tag",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    series_uid = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    task_id = table.Column<int>(type: "int", nullable: true),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_calendar_series_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_calendar_series_tag_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_calendar_series_tag_t_task_task_id",
                        column: x => x.task_id,
                        principalTable: "t_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_t_calendar_series_tag_t_user_user_id",
                        column: x => x.user_id,
                        principalTable: "t_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_calendar_series_tag_attribute",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    calendar_series_tag_id = table.Column<int>(type: "int", nullable: false),
                    time_entry_field_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_calendar_series_tag_attribute", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_calendar_series_tag_attribute_t_calendar_series_tag_calendar_series_tag_id",
                        column: x => x.calendar_series_tag_id,
                        principalTable: "t_calendar_series_tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_calendar_series_tag_attribute_t_time_entry_field_time_entry_field_id",
                        column: x => x.time_entry_field_id,
                        principalTable: "t_time_entry_field",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry",
                columns: new[] { "organization_id", "user_id", "source_series_uid", "source_occurrence_start_utc" },
                unique: true,
                filter: "[source_series_uid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_series_tag_organization_id_user_id_series_uid",
                table: "t_calendar_series_tag",
                columns: new[] { "organization_id", "user_id", "series_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_series_tag_task_id",
                table: "t_calendar_series_tag",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_series_tag_user_id",
                table: "t_calendar_series_tag",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_series_tag_attribute_calendar_series_tag_id_time_entry_field_id",
                table: "t_calendar_series_tag_attribute",
                columns: new[] { "calendar_series_tag_id", "time_entry_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_calendar_series_tag_attribute_time_entry_field_id",
                table: "t_calendar_series_tag_attribute",
                column: "time_entry_field_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_calendar_series_tag_attribute");

            migrationBuilder.DropTable(
                name: "t_calendar_series_tag");

            migrationBuilder.DropIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "source",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "source_event_id",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "source_occurrence_start_utc",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "source_series_uid",
                table: "t_time_entry");
        }
    }
}
