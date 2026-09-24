using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caribe.AccesoDatos.Migrations
{
    /// <summary>
    /// Tiempo de importacion por vehiculo, en semanas. Ambas columnas
    /// son nullable: los vehiculos que ya existen quedan sin tiempo y
    /// la ficha simplemente no lo muestra.
    /// </summary>
    public partial class TiempoImportacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "semanas_importacion_max",
                table: "vehiculos",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "semanas_importacion_min",
                table: "vehiculos",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "semanas_importacion_max",
                table: "vehiculos");

            migrationBuilder.DropColumn(
                name: "semanas_importacion_min",
                table: "vehiculos");
        }
    }
}
