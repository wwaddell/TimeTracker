using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TimeTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_user_organization_t_organization_role_organization_role_id",
                table: "t_user_organization");

            migrationBuilder.DropIndex(
                name: "IX_t_user_organization_organization_role_id",
                table: "t_user_organization");

            migrationBuilder.DropIndex(
                name: "IX_t_user_external_id",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "organization_role_id",
                table: "t_user_organization");

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                table: "t_user",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "t_organization",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "t_type_right",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_type_right", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "t_user_organization_role",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_organization_id = table.Column<int>(type: "int", nullable: false),
                    organization_role_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_user_organization_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_user_organization_role_t_organization_role_organization_role_id",
                        column: x => x.organization_role_id,
                        principalTable: "t_organization_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_user_organization_role_t_user_organization_user_organization_id",
                        column: x => x.user_organization_id,
                        principalTable: "t_user_organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_organization_role_right",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organization_role_id = table.Column<int>(type: "int", nullable: false),
                    right = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_organization_role_right", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_organization_role_right_t_organization_role_organization_role_id",
                        column: x => x.organization_role_id,
                        principalTable: "t_organization_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_t_organization_role_right_t_type_right_right",
                        column: x => x.right,
                        principalTable: "t_type_right",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "t_type_right",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { 1, "manage_organization", "Edit organization details.", "Manage organization" },
                    { 2, "manage_users", "Invite members and assign their roles.", "Manage users" },
                    { 3, "manage_roles", "Create and edit roles and their rights.", "Manage roles" },
                    { 4, "manage_fields", "Configure the time-entry fields.", "Manage fields" },
                    { 5, "view_reports", "View time reports and summaries.", "View reports" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_user_email",
                table: "t_user",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_external_id",
                table: "t_user",
                column: "external_id",
                unique: true,
                filter: "[external_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_t_organization_role_right_organization_role_id_right",
                table: "t_organization_role_right",
                columns: new[] { "organization_role_id", "right" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_organization_role_right_right",
                table: "t_organization_role_right",
                column: "right");

            migrationBuilder.CreateIndex(
                name: "IX_t_type_right_code",
                table: "t_type_right",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_role_organization_role_id",
                table: "t_user_organization_role",
                column: "organization_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_role_user_organization_id_organization_role_id",
                table: "t_user_organization_role",
                columns: new[] { "user_organization_id", "organization_role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_organization_role_right");

            migrationBuilder.DropTable(
                name: "t_user_organization_role");

            migrationBuilder.DropTable(
                name: "t_type_right");

            migrationBuilder.DropIndex(
                name: "IX_t_user_email",
                table: "t_user");

            migrationBuilder.DropIndex(
                name: "IX_t_user_external_id",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "description",
                table: "t_organization");

            migrationBuilder.AddColumn<int>(
                name: "organization_role_id",
                table: "t_user_organization",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                table: "t_user",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_user_organization_organization_role_id",
                table: "t_user_organization",
                column: "organization_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_user_external_id",
                table: "t_user",
                column: "external_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_t_user_organization_t_organization_role_organization_role_id",
                table: "t_user_organization",
                column: "organization_role_id",
                principalTable: "t_organization_role",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
