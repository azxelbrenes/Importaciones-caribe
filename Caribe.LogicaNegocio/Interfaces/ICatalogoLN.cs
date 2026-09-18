using Caribe.LogicaNegocio.Dtos.Catalogos;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface ICatalogoLN
{
    Task<Respuesta<IEnumerable<MarcaDto>>> ListarMarcasAsync(
        bool soloActivas = true, CancellationToken ct = default);

    Task<Respuesta<IEnumerable<ModeloDto>>> ListarModelosAsync(
        int? marcaId, CancellationToken ct = default);

    Task<Respuesta<int>> CrearMarcaAsync(
        CrearMarcaDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<int>> CrearModeloAsync(
        CrearModeloDto dto, string usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Opciones de los desplegables. No es async porque sale de los
    /// enums, no de la base.
    /// </summary>
    Respuesta<IEnumerable<OpcionDto>> ListarOpciones(string tipo);
}
