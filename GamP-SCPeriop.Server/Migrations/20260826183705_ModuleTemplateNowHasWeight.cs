using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class ModuleTemplateNowHasWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Weight",
                table: "ModuleTemplates",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 0f);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 0f);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 0f);

            migrationBuilder.UpdateData(
                table: "ModuleTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ModuleTemplates");
        }
    }
}
