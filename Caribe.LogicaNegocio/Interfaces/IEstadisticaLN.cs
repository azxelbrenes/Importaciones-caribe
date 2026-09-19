using Caribe.LogicaNegocio.Dtos.Estadisticas;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface IEstadisticaLN
{
    Task<Respuesta<ResumenDto>> ResumenAsync(CancellationToken ct = default);

    Task<Respuesta<IEnumerable<VentaMesDto>>> VentasPorMesAsync(
        int meses = 6, CancellationToken ct = default);

    /// <summary>
    /// Embudo de solicitudes. Suma los registros vivos mas los
    /// historicos de las que ya se purgaron.
    /// </summary>
    Task<Respuesta<IEnumerable<EtapaPipelineDto>>> PipelineAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Marcas mas pedidas en solicitudes. Le dice al dueno que traer
    /// aunque todavia no lo tenga en catalogo.
    /// </summary>
    Task<Respuesta<IEnumerable<MarcaSolicitadaDto>>> MarcasMasSolicitadasAsync(
        int top = 8, CancellationToken ct = default);
}
