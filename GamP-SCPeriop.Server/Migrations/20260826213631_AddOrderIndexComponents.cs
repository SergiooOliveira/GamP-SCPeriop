using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ModuleComponents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ComponentTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 7,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 10,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 11,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 12,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 13,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 14,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 15,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 17,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 18,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 19,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 20,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ComponentTemplates",
                keyColumn: "Id",
                keyValue: 21,
                column: "OrderIndex",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ModuleComponents");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ComponentTemplates");
        }
    }
}
