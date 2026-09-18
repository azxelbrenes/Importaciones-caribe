using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface IFotoLN
{
    Task<Respuesta<FotoSubidaDto>> SubirAsync(
        int vehiculoId, Stream contenido, string nombreOriginal,
        string usuarioId, CancellationToken ct = default);

    Task<Respuesta<IEnumerable<FotoSubidaDto>>> ListarAsync(
        int vehiculoId, CancellationToken ct = default);

    Task<Respuesta<bool>> EliminarAsync(
        int fotoId, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> MarcarPortadaAsync(
        int fotoId, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> ReordenarAsync(
        ReordenarFotosDto dto, string usuarioId, CancellationToken ct = default);
}
