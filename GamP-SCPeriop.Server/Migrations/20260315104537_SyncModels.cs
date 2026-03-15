using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class SyncModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Enrollments_Pathway_PathwayId",
            //    table: "Enrollments");

            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_Pathway",
            //    table: "Pathway");

            //migrationBuilder.RenameTable(
            //    name: "Pathway",
            //    newName: "Pathways");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_Pathways",
            //    table: "Pathways",
            //    column: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Enrollments_Pathways_PathwayId",
            //    table: "Enrollments",
            //    column: "PathwayId",
            //    principalTable: "Pathways",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Enrollments_Pathways_PathwayId",
            //    table: "Enrollments");

            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_Pathways",
            //    table: "Pathways");

            //migrationBuilder.RenameTable(
            //    name: "Pathways",
            //    newName: "Pathway");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_Pathway",
            //    table: "Pathway",
            //    column: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Enrollments_Pathway_PathwayId",
            //    table: "Enrollments",
            //    column: "PathwayId",
            //    principalTable: "Pathway",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }
    }
}
