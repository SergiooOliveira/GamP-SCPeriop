using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullPathwayId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules");

            migrationBuilder.AlterColumn<int>(
                name: "PathwayId",
                table: "Modules",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules",
                column: "PathwayId",
                principalTable: "Pathways",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules");

            migrationBuilder.AlterColumn<int>(
                name: "PathwayId",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules",
                column: "PathwayId",
                principalTable: "Pathways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
