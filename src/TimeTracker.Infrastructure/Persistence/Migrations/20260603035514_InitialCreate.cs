using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_organization",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "t_type_field_data_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_type_field_data_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "t_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    external_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "t_organization_role",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_organization_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_organization_role_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_task",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    is_complete = table.Column<bool>(type: "bit", nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_task", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_task_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_t_task_t_user_user_id",
                        column: x => x.user_id,
                        principalTable: "t_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_time_entry_field",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    organization_role_id = table.Column<int>(type: "int", nullable: true),
                    field_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    data_type = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_time_entry_field", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_time_entry_field_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_time_entry_field_t_organization_role_organization_role_id",
                        column: x => x.organization_role_id,
                        principalTable: "t_organization_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_time_entry_field_t_type_field_data_type_data_type",
                        column: x => x.data_type,
                        principalTable: "t_type_field_data_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_user_organization",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    organization_role_id = table.Column<int>(type: "int", nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user_organization", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_user_organization_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_user_organization_t_organization_role_organization_role_id",
                        column: x => x.organization_role_id,
                        principalTable: "t_organization_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_t_user_organization_t_user_user_id",
                        column: x => x.user_id,
                        principalTable: "t_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_time_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    task_id = table.Column<int>(type: "int", nullable: true),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    duration_minutes = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_time_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_time_entry_t_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "t_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_time_entry_t_task_task_id",
                        column: x => x.task_id,
                        principalTable: "t_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_t_time_entry_t_user_user_id",
                        column: x => x.user_id,
                        principalTable: "t_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_time_entry_field_option",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    time_entry_field_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_time_entry_field_option", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_time_entry_field_option_t_time_entry_field_time_entry_field_id",
                        column: x => x.time_entry_field_id,
                        principalTable: "t_time_entry_field",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_time_entry_attribute",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    time_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    time_entry_field_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_time_entry_attribute", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_time_entry_attribute_t_time_entry_field_time_entry_field_id",
                        column: x => x.time_entry_field_id,
                        principalTable: "t_time_entry_field",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_time_entry_attribute_t_time_entry_time_entry_id",
                        column: x => x.time_entry_id,
                        principalTable: "t_time_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "t_type_field_data_type",
                columns: new[] { "id", "code", "name" },
                values: new object[,]
                {
                    { 1, "text", "Text" },
                    { 2, "number", "Number" },
                    { 3, "date", "Date" },
                    { 4, "boolean", "Boolean" },
                    { 5, "select", "Select" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_organization_name",
                table: "t_organization",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_t_organization_role_organization_id_name",
                table: "t_organization_role",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_task_organization_id",
                table: "t_task",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_task_user_id_is_complete",
                table: "t_task",
                columns: new[] { "user_id", "is_complete" });

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_organization_id_entry_date",
                table: "t_time_entry",
                columns: new[] { "organization_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_task_id",
                table: "t_time_entry",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_user_id_entry_date",
                table: "t_time_entry",
                columns: new[] { "user_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_attribute_time_entry_field_id",
                table: "t_time_entry_attribute",
                column: "time_entry_field_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_attribute_time_entry_id_time_entry_field_id",
                table: "t_time_entry_attribute",
                columns: new[] { "time_entry_id", "time_entry_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_field_data_type",
                table: "t_time_entry_field",
                column: "data_type");

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_field_organization_id_field_key",
                table: "t_time_entry_field",
                columns: new[] { "organization_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_field_organization_role_id",
                table: "t_time_entry_field",
                column: "organization_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_time_entry_field_option_time_entry_field_id_sort_order",
                table: "t_time_entry_field_option",
                columns: new[] { "time_entry_field_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_t_type_field_data_type_code",
                table: "t_type_field_data_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_user_external_id",
                table: "t_user",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_organization_id",
                table: "t_user_organization",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_organization_role_id",
                table: "t_user_organization",
                column: "organization_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_user_id_organization_id",
                table: "t_user_organization",
                columns: new[] { "user_id", "organization_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_time_entry_attribute");

            migrationBuilder.DropTable(
                name: "t_time_entry_field_option");

            migrationBuilder.DropTable(
                name: "t_user_organization");

            migrationBuilder.DropTable(
                name: "t_time_entry");

            migrationBuilder.DropTable(
                name: "t_time_entry_field");

            migrationBuilder.DropTable(
                name: "t_task");

            migrationBuilder.DropTable(
                name: "t_organization_role");

            migrationBuilder.DropTable(
                name: "t_type_field_data_type");

            migrationBuilder.DropTable(
                name: "t_user");

            migrationBuilder.DropTable(
                name: "t_organization");
        }
    }
}
