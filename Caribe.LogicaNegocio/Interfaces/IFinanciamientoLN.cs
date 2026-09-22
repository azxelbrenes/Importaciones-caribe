using Caribe.LogicaNegocio.Dtos.Financiamiento;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface IFinanciamientoLN
{
    Task<Respuesta<ConfiguracionFinanciamientoDto>> ObtenerAsync(
        CancellationToken ct = default);

    Task<Respuesta<bool>> ActualizarAsync(
        ActualizarFinanciamientoDto dto, string usuarioId,
        CancellationToken ct = default);

    /// <summary>
    /// Lo que necesita el sitio publico: si se ofrece, prima y plazos.
    /// </summary>
    Task<Respuesta<FinanciamientoPublicoDto>> ObtenerPublicoAsync(
        CancellationToken ct = default);
}
