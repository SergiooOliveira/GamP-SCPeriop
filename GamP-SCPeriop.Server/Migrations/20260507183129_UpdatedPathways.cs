using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPathways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pathways",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumPassScore = table.Column<int>(type: "int", nullable: false),
                    MinimumApprovalScore = table.Column<int>(type: "int", nullable: false),
                    ProfessorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pathways", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    University = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PathwayId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ProfessorId = table.Column<int>(type: "int", nullable: false),
                    PathwayId = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PdfFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleComponents_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "Id", "MinimumApprovalScore", "MinimumPassScore", "ProfessorId", "Title" },
                values: new object[,]
                {
                    { 1, 75, 50, 9, "Enfermagem Cirúrgica" },
                    { 2, 80, 50, 10, "Anestesia Básica" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "Password", "Role", "University" },
                values: new object[,]
                {
                    { 6, "miguel@ipca.com", "Miguel Teixeira", "123", 0, "IPCA" },
                    { 7, "a100@alunos.ipca.pt", "Rúben Peixoto", "123", 1, "IPCA" },
                    { 8, "professorTeste@ipca.pt", "Teste de nome", "123", 0, "IPCA" },
                    { 9, "armando.costa@hospital.pt", "Dr. Armando Costa", "123", 0, "Hospital Central" },
                    { 10, "beatriz.sousa@hospital.pt", "Enf. Beatriz Sousa", "123", 0, "Hospital Central" },
                    { 11, "a101@alunos.ipca.pt", "Ana Silva", "123", 1, "Universidade do Minho" },
                    { 12, "a102@alunos.ipca.pt", "Carlos Martins", "123", 1, "Universidade do Porto" }
                });

            migrationBuilder.InsertData(
                table: "Enrollments",
                columns: new[] { "Id", "EndDate", "PathwayId", "ProfessorId", "ProgressPercentage", "StudentId" },
                values: new object[,]
                {
                    { 3, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 10, 80, 11 },
                    { 4, new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 9, 5, 12 }
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "PathwayId", "Title" },
                values: new object[,]
                {
                    { 1, 1, "Módulo Teórico - Preparação" },
                    { 2, 1, "Módulo Prático - Bloco Operatório" },
                    { 3, 2, "Módulo Único - Fármacos" }
                });

            migrationBuilder.InsertData(
                table: "ModuleComponents",
                columns: new[] { "Id", "ModuleId", "PdfFilePath", "Stage", "Status", "Title" },
                values: new object[,]
                {
                    { 1, 1, "", 1, 0, "Guia de Higienização" },
                    { 2, 2, "", 2, 0, "Checklist Cirúrgica" },
                    { 3, 1, "https://example.com/manual.pdf", 1, 0, "Manual de Acolhimento" },
                    { 4, 1, "", 2, 0, "Checklist de Segurança (OMS)" },
                    { 5, 2, "", 4, 0, "Preparação da Sala Operatória" },
                    { 6, 2, "", 5, 0, "Circulação na Sala" },
                    { 7, 3, "https://example.com/farmacos.pdf", 1, 0, "Tabela de Fármacos de Emergência" },
                    { 8, 3, "", 3, 0, "Preparação do Ventilação" },
                    { 9, 3, "", 5, 0, "Entubação Endotraqueal" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PathwayId",
                table: "Enrollments",
                column: "PathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProfessorId",
                table: "Enrollments",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleComponents_ModuleId",
                table: "ModuleComponents",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_PathwayId",
                table: "Modules",
                column: "PathwayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "ModuleComponents");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Pathways");
        }
    }
}
