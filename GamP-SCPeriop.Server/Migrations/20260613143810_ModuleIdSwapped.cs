using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class ModuleIdSwapped : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 10,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 11,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 12,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 13,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 14,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 15,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 16,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 17,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 18,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 19,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 20,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 21,
                column: "ModuleId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 22,
                column: "ModuleId",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 10,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 11,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 12,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 13,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 14,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 15,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 16,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 17,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 18,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 19,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 20,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 21,
                column: "ModuleId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 22,
                column: "ModuleId",
                value: 4);
        }
    }
}
