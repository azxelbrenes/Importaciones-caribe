using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Financiamiento;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

[Route("api/financiamiento")]
[Authorize(Roles = Roles.Gestion)]
public class FinanciamientoController : ControladorBase
{
    private readonly IFinanciamientoLN _ln;

    public FinanciamientoController(IFinanciamientoLN ln) => _ln = ln;

    /// <summary>
    /// Simulador del sitio publico. Devuelve null si el financiamiento
    /// esta apagado, y el frontend simplemente no muestra la seccion.
    /// </summary>
    [HttpGet("simular")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Simular(
        [FromQuery] decimal precio, CancellationToken ct)
        => Resolver(await _ln.SimularAsync(precio, ct));

    [HttpGet("configuracion")]
    public async Task<IActionResult> Configuracion(CancellationToken ct)
        => Resolver(await _ln.ObtenerAsync(ct));

    /// <summary>
    /// Cambiar la tasa es solo del propietario: tiene implicaciones
    /// legales bajo la Ley 9859 y hay que poder responder por ella.
    /// </summary>
    [HttpPut("configuracion")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> Actualizar(
        [FromBody] ActualizarFinanciamientoDto dto, CancellationToken ct)
        => Resolver(await _ln.ActualizarAsync(dto, UsuarioActual, ct));
}
