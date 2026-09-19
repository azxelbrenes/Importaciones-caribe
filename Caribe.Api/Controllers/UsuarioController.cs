using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

/// <summary>
/// Todo el controlador es del propietario: crear y quitar accesos es
/// la accion con mas alcance del sistema.
/// </summary>
[Route("api/usuarios")]
[Authorize(Roles = Roles.SuperAdministrador)]
public class UsuarioController : ControladorBase
{
    private readonly IUsuarioLN _ln;

    public UsuarioController(IUsuarioLN ln) => _ln = ln;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Resolver(await _ln.ListarAsync(ct));

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> ActivarDesactivar(
        string id, [FromQuery] bool activo, CancellationToken ct)
        => Resolver(await _ln.ActivarDesactivarAsync(id, activo, UsuarioActual, ct));

    [HttpPost("{id}/restablecer-password")]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> RestablecerPassword(
        string id, CancellationToken ct)
        => Resolver(await _ln.RestablecerPasswordAsync(id, UsuarioActual, ct));

    /// <summary>
    /// Cambiar la propia contrasena. Cualquier rol autenticado puede,
    /// asi que rompe el [Authorize] de la clase.
    /// </summary>
    [HttpPost("cambiar-password")]
    [Authorize]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> CambiarPassword(
        [FromBody] CambiarPasswordDto dto, CancellationToken ct)
        => Resolver(await _ln.CambiarPasswordAsync(dto, UsuarioActual, ct));

    // ══════════════════ INVITACIONES ══════════════════

    [HttpPost("invitaciones")]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> Invitar(
        [FromBody] CrearInvitacionDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.InvitarAsync(dto, UsuarioActual, ct));

    [HttpGet("invitaciones")]
    public async Task<IActionResult> ListarInvitaciones(CancellationToken ct)
        => Resolver(await _ln.ListarInvitacionesAsync(ct));

    [HttpDelete("invitaciones/{id:int}")]
    public async Task<IActionResult> RevocarInvitacion(int id, CancellationToken ct)
        => Resolver(await _ln.RevocarInvitacionAsync(id, UsuarioActual, ct));
}
