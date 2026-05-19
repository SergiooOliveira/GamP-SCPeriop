using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedEvaluationComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ModuleComponents");

            migrationBuilder.CreateTable(
                name: "ComponentEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ModuleComponentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentEvaluations_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentEvaluations_ModuleComponents_ModuleComponentId",
                        column: x => x.ModuleComponentId,
                        principalTable: "ModuleComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentEvaluations_EnrollmentId",
                table: "ComponentEvaluations",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentEvaluations_ModuleComponentId",
                table: "ComponentEvaluations",
                column: "ModuleComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentEvaluations");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ModuleComponents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 1,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 2,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 3,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 4,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 5,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 6,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 7,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 8,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 9,
                column: "Status",
                value: 0);
        }
    }
}
