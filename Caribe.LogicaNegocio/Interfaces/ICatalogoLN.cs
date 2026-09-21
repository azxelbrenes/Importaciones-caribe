using Caribe.LogicaNegocio.Dtos.Catalogos;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface ICatalogoLN
{
    // ── Lectura ──

    Task<Respuesta<IEnumerable<MarcaDto>>> ListarMarcasAsync(
        bool soloActivas = true, CancellationToken ct = default);

    Task<Respuesta<IEnumerable<ModeloDto>>> ListarModelosAsync(
        int? marcaId, CancellationToken ct = default);

    // ── Crear ──

    Task<Respuesta<int>> CrearMarcaAsync(
        CrearMarcaDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<int>> CrearModeloAsync(
        CrearModeloDto dto, string usuarioId, CancellationToken ct = default);

    // ── Desactivar ──

    /// <summary>
    /// Al desactivar una marca, sus modelos tambien se desactivan:
    /// dejarlos activos permitiria seleccionar un modelo de una marca
    /// que ya no se ofrece.
    /// </summary>
    Task<Respuesta<bool>> ActivarMarcaAsync(
        int id, bool activa, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> ActivarModeloAsync(
        int id, bool activo, string usuarioId, CancellationToken ct = default);

    // ── Eliminar ──

    /// <summary>
    /// Solo si ningun vehiculo la usa. Con vehiculos asociados,
    /// borrarla romperia el catalogo.
    /// </summary>
    Task<Respuesta<bool>> EliminarMarcaAsync(
        int id, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> EliminarModeloAsync(
        int id, string usuarioId, CancellationToken ct = default);

    // ── Mantenimiento ──

    /// <summary>
    /// Fusiona los duplicados que quedaron de antes de la
    /// normalizacion y limpia los nombres con espacios de sobra.
    /// Se ejecuta una sola vez.
    /// </summary>
    Task<Respuesta<LimpiezaDto>> LimpiarDuplicadosAsync(
        string usuarioId, CancellationToken ct = default);

    // ── Opciones ──

    Respuesta<IEnumerable<OpcionDto>> ListarOpciones(string tipo);
}
