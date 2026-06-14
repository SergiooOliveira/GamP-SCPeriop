using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class ImagemEstruturaAvaliacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentComponentId",
                table: "ModuleComponents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ProgressPercentage", "StartDate" },
                values: new object[] { 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ProgressPercentage", "StartDate" },
                values: new object[] { 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 1,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 2,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 3,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 4,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 5,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 6,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 7,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 8,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 9,
                column: "ParentComponentId",
                value: null);

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "PathwayId", "Title" },
                values: new object[] { 4, 2, "UT1 - Introdução à Anestesia" });

            migrationBuilder.InsertData(
                table: "ModuleComponents",
                columns: new[] { "Id", "Description", "ModuleId", "ParentComponentId", "PdfFilePath", "Stage", "Title" },
                values: new object[,]
                {
                    { 10, null, 4, null, null, 5, "Demonstra conhecimento das Normas de prevenção da Infeção do Local Cirúrgico" },
                    { 11, null, 4, null, null, 5, "Procede aos devidos registos clínicos informáticos no intraoperatório" },
                    { 12, null, 4, null, null, 5, "Sclínico" },
                    { 16, null, 4, null, null, 5, "Valida adequadamente a administração de medicação no sistema Ghaf;" },
                    { 17, null, 4, null, null, 5, "Ghaf" },
                    { 22, null, 4, null, null, 5, "Regista adequadamente a administração de estupefacientes em folha própria (Mod.3)" },
                    { 13, null, 4, 12, null, 5, "Regista Diagnósticos de Enfermagem adequadamente" },
                    { 14, null, 4, 12, null, 5, "Regista Atitudes terapêuticas adequadamente" },
                    { 15, null, 4, 12, null, 5, "Regista SV (incluindo temperatura corporal) e Glicemia Capilar de acordo com as normas em vigor" },
                    { 18, null, 4, 17, null, 5, "Administração de Antibioterapia, de acordo com a norma em vigor" },
                    { 19, null, 4, 17, null, 5, "Efetua débitos ao armazém" },
                    { 20, null, 4, 17, null, 5, "Efetua devoluções ao armazém" },
                    { 21, null, 4, 17, null, 5, "Efetua pedidos de dietas para o utente e acompanhante (quando aplicável)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleComponents_ParentComponentId",
                table: "ModuleComponents",
                column: "ParentComponentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleComponents_ModuleComponents_ParentComponentId",
                table: "ModuleComponents",
                column: "ParentComponentId",
                principalTable: "ModuleComponents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleComponents_ModuleComponents_ParentComponentId",
                table: "ModuleComponents");

            migrationBuilder.DropIndex(
                name: "IX_ModuleComponents_ParentComponentId",
                table: "ModuleComponents");

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "ParentComponentId",
                table: "ModuleComponents");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Enrollments");

            migrationBuilder.UpdateData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 3,
                column: "ProgressPercentage",
                value: 80);

            migrationBuilder.UpdateData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 4,
                column: "ProgressPercentage",
                value: 5);
        }
    }
}
