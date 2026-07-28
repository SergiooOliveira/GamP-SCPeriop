using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class Createrelationsforpathwaytemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 9);

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
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.InsertData(
                table: "PathwayTemplates",
                columns: new[] { "Id", "Description", "IsAdminBase", "MinimumApprovalScore", "SupervisorOwnerId", "Title" },
                values: new object[,]
                {
                    { 1, "Molde base que inclui preparação teórica e observação prática.", true, 100, null, "Molde Standard - Bloco Operatório" },
                    { 2, "Focado exclusivamente em procedimentos de anestesiologia.", true, 100, null, "Molde Avançado - Anestesia" }
                });

            migrationBuilder.InsertData(
                table: "ModuleTemplates",
                columns: new[] { "Id", "PathwayTemplateId", "Title" },
                values: new object[,]
                {
                    { 1, 1, "Módulo Teórico - Preparação" },
                    { 2, 1, "Módulo Prático - Bloco Operatório" },
                    { 3, 2, "Módulo Único - Fármacos" },
                    { 4, 2, "UT1 - Introdução à Anestesia" }
                });

            migrationBuilder.InsertData(
                table: "ComponentTemplates",
                columns: new[] { "Id", "Description", "ModuleTemplateId", "ParentComponentTemplateId", "Stage", "Title", "Weight" },
                values: new object[,]
                {
                    { 1, null, 1, null, 1, "Guia de Higienização", 50 },
                    { 2, null, 2, null, 2, "Checklist Cirúrgica", 100 },
                    { 3, null, 1, null, 1, "Manual de Acolhimento", 50 },
                    { 7, null, 3, null, 1, "Tabela de Fármacos de Emergência", 100 },
                    { 10, null, 4, null, 2, "Demonstra conhecimento das Normas de prevenção", 20 },
                    { 11, null, 4, null, 2, "Procede aos devidos registos clínicos", 20 },
                    { 12, null, 4, null, 2, "Sclínico", 30 },
                    { 17, null, 4, null, 2, "Ghaf", 30 },
                    { 13, null, 4, 12, 2, "Regista Diagnósticos de Enfermagem", 34 },
                    { 14, null, 4, 12, 2, "Regista Atitudes terapêuticas", 33 },
                    { 15, null, 4, 12, 2, "Regista SV e Glicemia Capilar", 33 },
                    { 18, null, 4, 17, 2, "Administração de Antibioterapia", 25 },
                    { 19, null, 4, 17, 2, "Efetua débitos ao armazém", 25 },
                    { 20, null, 4, 17, 2, "Efetua devoluções ao armazém", 25 },
                    { 21, null, 4, 17, 2, "Efetua pedidos de dietas", 25 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PathwayTemplates",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PathwayTemplates",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "PathwayId", "Title", "Weight" },
                values: new object[,]
                {
                    { 1, 1, "Módulo Teórico - Preparação", 1 },
                    { 2, 1, "Módulo Prático - Bloco Operatório", 1 },
                    { 3, 2, "Módulo Único - Fármacos", 1 },
                    { 4, 2, "UT1 - Introdução à Anestesia", 1 }
                });

            migrationBuilder.InsertData(
                table: "ModuleComponents",
                columns: new[] { "Id", "Description", "ModuleId", "ParentComponentId", "PdfFilePath", "Stage", "Title", "Weight" },
                values: new object[,]
                {
                    { 1, null, 1, null, "", 1, "Guia de Higienização", 1 },
                    { 2, null, 2, null, "", 2, "Checklist Cirúrgica", 1 },
                    { 3, null, 1, null, "https://example.com/manual.pdf", 1, "Manual de Acolhimento", 1 },
                    { 4, null, 1, null, "", 2, "Checklist de Segurança (OMS)", 1 },
                    { 5, null, 2, null, "", 4, "Preparação da Sala Operatória", 1 },
                    { 6, null, 2, null, "", 5, "Circulação na Sala", 1 },
                    { 7, null, 3, null, "https://example.com/farmacos.pdf", 1, "Tabela de Fármacos de Emergência", 1 },
                    { 8, null, 3, null, "", 3, "Preparação do Ventilação", 1 },
                    { 9, null, 3, null, "", 5, "Entubação Endotraqueal", 1 },
                    { 10, null, 4, null, null, 2, "Demonstra conhecimento das Normas de prevenção da Infeção do Local Cirúrgico", 1 },
                    { 11, null, 4, null, null, 2, "Procede aos devidos registos clínicos informáticos no intraoperatório", 1 },
                    { 12, null, 4, null, null, 2, "Sclínico", 1 },
                    { 16, null, 4, null, null, 2, "Valida adequadamente a administração de medicação no sistema Ghaf;", 1 },
                    { 17, null, 4, null, null, 2, "Ghaf", 1 },
                    { 22, null, 4, null, null, 2, "Regista adequadamente a administração de estupefacientes em folha própria (Mod.3)", 1 },
                    { 13, null, 4, 12, null, 2, "Regista Diagnósticos de Enfermagem adequadamente", 1 },
                    { 14, null, 4, 12, null, 2, "Regista Atitudes terapêuticas adequadamente", 1 },
                    { 15, null, 4, 12, null, 2, "Regista SV (incluindo temperatura corporal) e Glicemia Capilar de acordo com as normas em vigor", 1 },
                    { 18, null, 4, 17, null, 2, "Administração de Antibioterapia, de acordo com a norma em vigor", 1 },
                    { 19, null, 4, 17, null, 2, "Efetua débitos ao armazém", 1 },
                    { 20, null, 4, 17, null, 2, "Efetua devoluções ao armazém", 1 },
                    { 21, null, 4, 17, null, 2, "Efetua pedidos de dietas para o utente e acompanhante (quando aplicável)", 1 }
                });
        }
    }
}
