using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class TeacherRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MissingSupervisorWarningLevel",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeadlineExtensionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    CurrentEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedById = table.Column<int>(type: "int", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineExtensionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadlineExtensionRequests_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeadlineExtensionRequests_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineExtensionRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SupervisorId",
                table: "Enrollments",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensionRequests_DecidedById",
                table: "DeadlineExtensionRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensionRequests_EnrollmentId_ModuleId_Stage_Status",
                table: "DeadlineExtensionRequests",
                columns: new[] { "EnrollmentId", "ModuleId", "Stage", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensionRequests_RequestedById",
                table: "DeadlineExtensionRequests",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_SupervisorId",
                table: "Enrollments",
                column: "SupervisorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Former Admins (2) are school professors: they become Teachers (3). Nobody becomes Support here.
            migrationBuilder.Sql("UPDATE Users SET Role = 3 WHERE Role = 2;");

            // Pathways built by a supervisor (role 0): that supervisor keeps grading its students
            migrationBuilder.Sql(@"
                UPDATE e SET e.SupervisorId = p.ProfessorId
                FROM Enrollments e
                JOIN Pathways p ON p.Id = e.PathwayId
                JOIN Users u ON u.Id = p.ProfessorId
                WHERE u.Role = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role IN (2, 3);");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_SupervisorId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "DeadlineExtensionRequests");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SupervisorId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "MissingSupervisorWarningLevel",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "Enrollments");
        }
    }
}
