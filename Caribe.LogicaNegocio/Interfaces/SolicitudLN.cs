using Caribe.LogicaNegocio.Dtos.Solicitudes;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface ISolicitudLN
{
    /// <summary>
    /// Formulario publico. Recibe la IP para el anti duplicado y para
    /// poder investigar envios automatizados.
    /// </summary>
    Task<Respuesta<int>> CrearAsync(
        CrearSolicitudDto dto, string? ip, CancellationToken ct = default);

    Task<Respuesta<Pagina<SolicitudDto>>> ListarAsync(
        FiltroSolicitudDto filtro, CancellationToken ct = default);

    Task<Respuesta<SolicitudDetalleDto>> BuscarPorIdAsync(
        int id, CancellationToken ct = default);

    Task<Respuesta<bool>> ActualizarAsync(
        ActualizarSolicitudDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<int>> AgregarNotaAsync(
        CrearNotaDto dto, string usuarioId, CancellationToken ct = default);

    // ── Limpieza de la bandeja ──

    /// <summary>
    /// Saca la solicitud de la bandeja sin borrarla. Es el metodo
    /// principal de limpieza: las solicitudes son historial comercial
    /// y alimentan las estadisticas de conversion.
    /// </summary>
    Task<Respuesta<bool>> ArchivarAsync(
        int id, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<int>> ArchivarCerradasAsync(
        int mesesAntiguedad, string usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Borra de verdad. Solo para spam, bots y duplicados obvios:
    /// esos ensucian la estadistica en sentido contrario.
    ///
    /// Antes de borrar, acumula el conteo en estadisticas historicas
    /// para que la tasa de conversion no se distorsione.
    /// </summary>
    Task<Respuesta<bool>> EliminarAsync(
        int id, string usuarioId, CancellationToken ct = default);
}