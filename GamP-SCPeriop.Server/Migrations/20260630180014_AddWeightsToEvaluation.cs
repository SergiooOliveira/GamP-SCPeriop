using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightsToEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "ModuleComponents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 5,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 6,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 7,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 8,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 9,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 10,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 11,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 12,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 13,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 14,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 15,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 16,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 17,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 18,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 19,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 20,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 21,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ModuleComponents",
                keyColumn: "Id",
                keyValue: 22,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ModuleComponents");
        }
    }
}
