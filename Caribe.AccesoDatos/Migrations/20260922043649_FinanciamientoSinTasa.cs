using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caribe.AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class FinanciamientoSinTasa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tasa_anual",
                table: "configuracion_financiamiento");

            migrationBuilder.DropColumn(
                name: "texto_legal",
                table: "configuracion_financiamiento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tasa_anual",
                table: "configuracion_financiamiento",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "texto_legal",
                table: "configuracion_financiamiento",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "configuracion_financiamiento",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "tasa_anual", "texto_legal" },
                values: new object[] { 0m, null });
        }
    }
}
