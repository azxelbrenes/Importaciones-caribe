using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caribe.AccesoDatos.Migrations
{
    /// <summary>
    /// Vuelve el interes del financiamiento, ahora como porcentaje que
    /// se suma una sola vez sobre lo financiado. Arranca en 15%; el
    /// propietario lo cambia desde el panel.
    /// </summary>
    public partial class InteresFinanciamiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_interes",
                table: "configuracion_financiamiento",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 15m);

            migrationBuilder.UpdateData(
                table: "configuracion_financiamiento",
                keyColumn: "id",
                keyValue: 1,
                column: "porcentaje_interes",
                value: 15m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "porcentaje_interes",
                table: "configuracion_financiamiento");
        }
    }
}
