using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedCurriculumData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "Id", "MinimumApprovalScore", "MinimumPassScore", "Title" },
                values: new object[,]
                {
                    { 1, 75, 50, "Enfermagem Cirúrgica" },
                    { 2, 80, 50, "Anestesia Básica" }
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
                columns: new[] { "Id", "ModuleId", "PdfFilePath", "Status", "Title" },
                values: new object[,]
                {
                    { 1, 1, "", 0, "Guia de Higienização" },
                    { 2, 2, "", 0, "Checklist Cirúrgica" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Pathways",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Pathways",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
