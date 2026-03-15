using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class LinkModulesToPathway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Enrollments_EnrollmentId",
                table: "Modules");

            migrationBuilder.AlterColumn<int>(
                name: "EnrollmentId",
                table: "Modules",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PathwayId",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_PathwayId",
                table: "Modules",
                column: "PathwayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Enrollments_EnrollmentId",
                table: "Modules",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules",
                column: "PathwayId",
                principalTable: "Pathways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Enrollments_EnrollmentId",
                table: "Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Pathways_PathwayId",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Modules_PathwayId",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PathwayId",
                table: "Modules");

            migrationBuilder.AlterColumn<int>(
                name: "EnrollmentId",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Enrollments_EnrollmentId",
                table: "Modules",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
