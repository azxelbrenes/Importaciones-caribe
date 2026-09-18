using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Dtos.Vehiculos;

namespace Caribe.LogicaNegocio.Comunes;

/// <summary>
/// Trozos de consulta que se repiten entre el catalogo publico y el
/// panel.
///
/// Se extraen aqui para que el filtro por marca se escriba una sola
/// vez: si estuviera duplicado y manana se agrega un criterio, es
/// facil actualizarlo en un lado y olvidarlo en el otro.
/// </summary>
public static class ConsultasVehiculo
{
    /// <summary>
    /// Filtros comunes. No incluye el de estado: el catalogo publico
    /// fuerza los visibles y el panel deja elegir.
    /// </summary>
    public static IQueryable<Vehiculo> AplicarFiltros(
        this IQueryable<Vehiculo> consulta, FiltroVehiculoDto f)
    {
        if (f.MarcaId is > 0)
            consulta = consulta.Where(v => v.MarcaId == f.MarcaId);

        if (f.ModeloId is > 0)
            consulta = consulta.Where(v => v.ModeloId == f.ModeloId);

        if (f.AnioDesde is > 0)
            consulta = consulta.Where(v => v.Anio >= f.AnioDesde);

        if (f.AnioHasta is > 0)
            consulta = consulta.Where(v => v.Anio <= f.AnioHasta);

        if (f.PrecioMin is > 0)
            consulta = consulta.Where(v => v.PrecioPublicado >= f.PrecioMin);

        if (f.PrecioMax is > 0)
            consulta = consulta.Where(v => v.PrecioPublicado <= f.PrecioMax);

        if (f.KilometrajeMax is > 0)
            consulta = consulta.Where(v => v.Kilometraje <= f.KilometrajeMax);

        if (f.Transmision.HasValue)
            consulta = consulta.Where(v => v.Transmision == (Transmision)f.Transmision.Value);

        if (f.Combustible.HasValue)
            consulta = consulta.Where(v => v.Combustible == (Combustible)f.Combustible.Value);

        if (f.Traccion.HasValue)
            consulta = consulta.Where(v => v.Traccion == (Traccion)f.Traccion.Value);

        if (f.AceptaFinanciamiento == true)
            consulta = consulta.Where(v => v.AceptaFinanciamiento);

        if (!string.IsNullOrWhiteSpace(f.Busqueda))
        {
            var texto = f.Busqueda.Trim().ToLower();

            // ILike de Postgres seria mas eficiente, pero ToLower
            // funciona en cualquier proveedor y con pocos miles de
            // registros la diferencia es imperceptible.
            consulta = consulta.Where(v =>
                v.Marca.Nombre.ToLower().Contains(texto) ||
                v.Modelo.Nombre.ToLower().Contains(texto) ||
                (v.Color != null && v.Color.ToLower().Contains(texto)));
        }

        return consulta;
    }

    public static IQueryable<Vehiculo> Ordenar(
        this IQueryable<Vehiculo> consulta, string? ordenarPor) => ordenarPor switch
    {
        "precio_asc"  => consulta.OrderBy(v => v.PrecioPublicado),
        "precio_desc" => consulta.OrderByDescending(v => v.PrecioPublicado),
        "km_asc"      => consulta.OrderBy(v => v.Kilometraje),
        "anio_desc"   => consulta.OrderByDescending(v => v.Anio),

        // Por defecto: destacados primero, despues los mas recientes.
        // Es el orden que le sirve al negocio, no el alfabetico.
        _ => consulta
            .OrderByDescending(v => v.Destacado)
            .ThenByDescending(v => v.PublicadoEn ?? v.CreadoEn)
    };
}
