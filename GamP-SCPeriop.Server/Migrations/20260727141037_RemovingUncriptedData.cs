using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemovingUncriptedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Pathways",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Pathways",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 12);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "Id", "EndDate", "PathwayId", "ProfessorId", "ProgressPercentage", "StartDate", "StudentId" },
                values: new object[,]
                {
                    { 3, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 10, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 11 },
                    { 4, new DateTime(2026, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 9, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 12 }
                });
        }
    }
}
