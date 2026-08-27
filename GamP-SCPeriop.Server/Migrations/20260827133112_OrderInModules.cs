using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class OrderInModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ModuleTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "OrderIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "OrderIndex",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ModuleTemplates");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Modules");
        }
    }
}
