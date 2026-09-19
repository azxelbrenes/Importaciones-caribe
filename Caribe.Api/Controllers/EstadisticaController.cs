using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caribe.Api.Controllers;

/// <summary>
/// Sin excepciones publicas: estas cifras revelan margenes, ingresos
/// y volumen de operacion. Es justo lo que la competencia querria
/// saber.
/// </summary>
[Route("api/estadisticas")]
[Authorize(Roles = Roles.Gestion)]
public class EstadisticaController : ControladorBase
{
    private readonly IEstadisticaLN _ln;

    public EstadisticaController(IEstadisticaLN ln) => _ln = ln;

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct)
        => Resolver(await _ln.ResumenAsync(ct));

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(
        [FromQuery] int meses = 6, CancellationToken ct = default)
        => Resolver(await _ln.VentasPorMesAsync(meses, ct));

    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline(CancellationToken ct)
        => Resolver(await _ln.PipelineAsync(ct));

    [HttpGet("marcas-solicitadas")]
    public async Task<IActionResult> MarcasSolicitadas(
        [FromQuery] int top = 8, CancellationToken ct = default)
        => Resolver(await _ln.MarcasMasSolicitadasAsync(top, ct));
}
