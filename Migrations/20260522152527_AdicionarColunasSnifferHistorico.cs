using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorRede.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarColunasSnifferHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IPDestino",
                table: "HistoricosRede",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PortaDestino",
                table: "HistoricosRede",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocolo",
                table: "HistoricosRede",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IPDestino",
                table: "HistoricosRede");

            migrationBuilder.DropColumn(
                name: "PortaDestino",
                table: "HistoricosRede");

            migrationBuilder.DropColumn(
                name: "Protocolo",
                table: "HistoricosRede");
        }
    }
}
