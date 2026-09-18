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
    /// Simula las cuotas para un precio. Devuelve null en el valor si
    /// el financiamiento no esta operativo: sin tasa puesta o sin
    /// activar, no se ofrece nada.
    /// </summary>
    Task<Respuesta<SimulacionDto?>> SimularAsync(
        decimal precio, CancellationToken ct = default);
}
