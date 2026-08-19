using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWeightToFloat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Weight",
                table: "Modules",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "Weight",
                table: "ModuleComponents",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "Weight",
                table: "ComponentTemplates",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 50f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 100f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 50f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 7,
                column: "Weight",
                value: 100f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 10,
                column: "Weight",
                value: 20f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 11,
                column: "Weight",
                value: 20f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 12,
                column: "Weight",
                value: 30f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 13,
                column: "Weight",
                value: 34f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 14,
                column: "Weight",
                value: 33f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 15,
                column: "Weight",
                value: 33f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 17,
                column: "Weight",
                value: 30f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 18,
                column: "Weight",
                value: 25f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 19,
                column: "Weight",
                value: 25f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 20,
                column: "Weight",
                value: 25f);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 21,
                column: "Weight",
                value: 25f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Weight",
                table: "Modules",
                type: "int",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "Weight",
                table: "ModuleComponents",
                type: "int",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "Weight",
                table: "ComponentTemplates",
                type: "int",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 50);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 100);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 50);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 7,
                column: "Weight",
                value: 100);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 10,
                column: "Weight",
                value: 20);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 11,
                column: "Weight",
                value: 20);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 12,
                column: "Weight",
                value: 30);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 13,
                column: "Weight",
                value: 34);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 14,
                column: "Weight",
                value: 33);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 15,
                column: "Weight",
                value: 33);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 17,
                column: "Weight",
                value: 30);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 18,
                column: "Weight",
                value: 25);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 19,
                column: "Weight",
                value: 25);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 20,
                column: "Weight",
                value: 25);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 21,
                column: "Weight",
                value: 25);
        }
    }
}
