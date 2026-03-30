using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamP_SCPeriop.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "Password", "Role", "University" },
                values: new object[,]
                {
                    { 6, "miguel@ipca.com", "Miguel Teixeira", "123", 0, "IPCA" },
                    { 7, "a100@alunos.ipca.pt", "Rúben Peixoto", "123", 1, "IPCA" },
                    { 8, "professorTeste@ipca.pt", "Teste de nome", "123", 0, "IPCA" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
