using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEnrollmentModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_ProfessorId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ProfessorId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProfessorId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Enrollments");

            migrationBuilder.CreateTable(
                name: "EnrollmentModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentModules_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_ProfessorId",
                table: "Pathways",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentModules_EnrollmentId",
                table: "EnrollmentModules",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentModules_ModuleId",
                table: "EnrollmentModules",
                column: "ModuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pathways_Users_ProfessorId",
                table: "Pathways",
                column: "ProfessorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pathways_Users_ProfessorId",
                table: "Pathways");

            migrationBuilder.DropTable(
                name: "EnrollmentModules");

            migrationBuilder.DropIndex(
                name: "IX_Pathways_ProfessorId",
                table: "Pathways");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ProfessorId",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProfessorId",
                table: "Enrollments",
                column: "ProfessorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_ProfessorId",
                table: "Enrollments",
                column: "ProfessorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
