using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

/// <summary>
/// Las interfaces viven en LogicaNegocio y no en Dominio porque
/// devuelven DTOs, que son un detalle de esta capa. El dominio no
/// sabe que existe una API.
/// </summary>
public interface IVehiculoLN
{
    // ── Publico ──

    Task<Respuesta<Pagina<VehiculoDto>>> ListarPublicoAsync(
        FiltroVehiculoDto filtro, CancellationToken ct = default);

    Task<Respuesta<VehiculoDetalleDto>> BuscarPorSlugAsync(
        string slug, CancellationToken ct = default);

    /// <summary>
    /// El destacado del inicio. Si no hay ninguno marcado, devuelve
    /// el mas reciente: la portada nunca queda vacia teniendo
    /// vehiculos publicados.
    /// </summary>
    Task<Respuesta<VehiculoDto?>> DestacadoAsync(CancellationToken ct = default);

    /// <summary>
    /// Slugs de los vehiculos visibles, con su ultima modificacion.
    /// Alimenta el sitemap: sin el, Google tendria que descubrir cada
    /// ficha navegando, y las nuevas tardarian semanas en aparecer.
    /// </summary>
    Task<Respuesta<IEnumerable<(string Slug, DateTimeOffset Actualizado)>>>
        SlugsPublicadosAsync(CancellationToken ct = default);

    // ── Panel ──

    Task<Respuesta<Pagina<VehiculoAdminDto>>> ListarAdminAsync(
        FiltroVehiculoDto filtro, CancellationToken ct = default);

    Task<Respuesta<VehiculoDetalleAdminDto>> BuscarPorIdAsync(
        int id, CancellationToken ct = default);

    Task<Respuesta<int>> CrearAsync(
        CrearVehiculoDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> ActualizarAsync(
        ActualizarVehiculoDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> CambiarEstadoAsync(
        CambiarEstadoDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> EliminarAsync(
        int id, string usuarioId, CancellationToken ct = default);
}
