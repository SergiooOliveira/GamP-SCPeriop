using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class TemplateTablesAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PathwayTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumApprovalScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PathwayTemplateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleTemplates_PathwayTemplates_PathwayTemplateId",
                        column: x => x.PathwayTemplateId,
                        principalTable: "PathwayTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    ModuleTemplateId = table.Column<int>(type: "int", nullable: false),
                    ParentComponentTemplateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentTemplates_ComponentTemplates_ParentComponentTemplateId",
                        column: x => x.ParentComponentTemplateId,
                        principalTable: "ComponentTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComponentTemplates_ModuleTemplates_ModuleTemplateId",
                        column: x => x.ModuleTemplateId,
                        principalTable: "ModuleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplates_ModuleTemplateId",
                table: "ComponentTemplates",
                column: "ModuleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplates_ParentComponentTemplateId",
                table: "ComponentTemplates",
                column: "ParentComponentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTemplates_PathwayTemplateId",
                table: "ModuleTemplates",
                column: "PathwayTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentTemplates");

            migrationBuilder.DropTable(
                name: "ModuleTemplates");

            migrationBuilder.DropTable(
                name: "PathwayTemplates");
        }
    }
}
