using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldIsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "t_type_time_entry_field",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill: any existing field with one of the seeded keys is system. Future
            // additions to this list become migration HasData seeds rather than ad-hoc
            // updates. Match is by field_key (lower-case, stable) so it survives renames
            // of the human-facing Label.
            migrationBuilder.Sql(@"
                UPDATE t_type_time_entry_field
                SET    is_system = 1
                WHERE  field_key IN ('project', 'billable', 'category');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_system",
                table: "t_type_time_entry_field");
        }
    }
}
