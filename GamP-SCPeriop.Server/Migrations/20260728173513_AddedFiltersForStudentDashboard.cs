using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedFiltersForStudentDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "Enrollments");
        }
    }
}
