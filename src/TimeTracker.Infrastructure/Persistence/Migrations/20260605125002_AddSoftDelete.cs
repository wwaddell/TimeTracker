using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_utc",
                table: "t_time_entry",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_utc",
                table: "t_task",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry",
                columns: new[] { "organization_id", "user_id", "source_series_uid", "source_occurrence_start_utc" },
                unique: true,
                filter: "[source_series_uid] IS NOT NULL AND [deleted_utc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "deleted_utc",
                table: "t_time_entry");

            migrationBuilder.DropColumn(
                name: "deleted_utc",
                table: "t_task");

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_organization_id_user_id_source_series_uid_source_occurrence_start_utc",
                table: "t_time_entry",
                columns: new[] { "organization_id", "user_id", "source_series_uid", "source_occurrence_start_utc" },
                unique: true,
                filter: "[source_series_uid] IS NOT NULL");
        }
    }
}
