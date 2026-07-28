using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfFilePathToComponentTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "ComponentTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 7,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 10,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 11,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 12,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 13,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 14,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 15,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 17,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 18,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 19,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 20,
                column: "PdfFilePath",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 21,
                column: "PdfFilePath",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "ComponentTemplates");
        }
    }
}
