using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace School_Management_System.Data.EfMigrations
{
    /// <inheritdoc />
    public partial class _20260307_WorkflowExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "archive_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    entity_type = table.Column<string>(type: "longtext", nullable: false),
                    original_entity_id = table.Column<long>(type: "bigint", nullable: true),
                    payload = table.Column<string>(type: "longtext", nullable: false),
                    deleted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_restored = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    restored_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    restored_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    notes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_archive_records_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_archive_records_users_restored_by_user_id",
                        column: x => x.restored_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "school_settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    school_name = table.Column<string>(type: "longtext", nullable: false),
                    school_code = table.Column<string>(type: "longtext", nullable: false),
                    school_address = table.Column<string>(type: "longtext", nullable: false),
                    principal_name = table.Column<string>(type: "longtext", nullable: false),
                    grading_setup = table.Column<string>(type: "longtext", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_settings", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_requirements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    requirement_name = table.Column<string>(type: "longtext", nullable: false),
                    is_submitted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    notes = table.Column<string>(type: "longtext", nullable: true),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_requirements_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_requirements_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_archive_records_deleted_by_user_id",
                table: "archive_records",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_archive_records_restored_by_user_id",
                table: "archive_records",
                column: "restored_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_requirements_student_id",
                table: "student_requirements",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_requirements_verified_by_user_id",
                table: "student_requirements",
                column: "verified_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "archive_records");

            migrationBuilder.DropTable(
                name: "school_settings");

            migrationBuilder.DropTable(
                name: "student_requirements");
        }
    }
}
