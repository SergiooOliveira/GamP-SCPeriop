using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserBadgeAndBadgeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_BadgeTemplates_BadgeTemplateId",
                table: "UserBadges");

            migrationBuilder.RenameColumn(
                name: "BadgeTemplateId",
                table: "UserBadges",
                newName: "BadgeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserBadges_BadgeTemplateId",
                table: "UserBadges",
                newName: "IX_UserBadges_BadgeId");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserBadges",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PathwayId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    TriggerValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.RenameColumn(
                name: "BadgeId",
                table: "UserBadges",
                newName: "BadgeTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_UserBadges_BadgeId",
                table: "UserBadges",
                newName: "IX_UserBadges_BadgeTemplateId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserBadges",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_BadgeTemplates_BadgeTemplateId",
                table: "UserBadges",
                column: "BadgeTemplateId",
                principalTable: "BadgeTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
