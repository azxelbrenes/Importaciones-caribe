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
    /// Para el sitio: si se ofrece, prima y plazos. Sin tasas.
    /// </summary>
    [HttpGet("publico")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Publico(CancellationToken ct)
        => Resolver(await _ln.ObtenerPublicoAsync(ct));

    [HttpGet("configuracion")]
    public async Task<IActionResult> Configuracion(CancellationToken ct)
        => Resolver(await _ln.ObtenerAsync(ct));

    /// <summary>Solo el propietario cambia las condiciones del negocio.</summary>
    [HttpPut("configuracion")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> Actualizar(
        [FromBody] ActualizarFinanciamientoDto dto, CancellationToken ct)
        => Resolver(await _ln.ActualizarAsync(dto, UsuarioActual, ct));
}
