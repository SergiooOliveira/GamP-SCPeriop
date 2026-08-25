using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class BadgeStoresPathwayTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Badges_PathwayId",
                table: "Badges",
                column: "PathwayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Badges_Pathways_PathwayId",
                table: "Badges",
                column: "PathwayId",
                principalTable: "Pathways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Badges_Pathways_PathwayId",
                table: "Badges");

            migrationBuilder.DropIndex(
                name: "IX_Badges_PathwayId",
                table: "Badges");
        }
    }
}
