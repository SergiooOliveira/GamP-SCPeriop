using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PathwayTemplateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    TriggerValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeTemplates");
        }
    }
}
