using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

/// <summary>
/// Gestion por defecto; las dos rutas publicas se marcan una por una.
///
/// Es al reves de lo comun —abierto salvo excepciones— a proposito:
/// si manana alguien agrega un endpoint y olvida el atributo, queda
/// protegido en vez de expuesto.
/// </summary>
[Route("api/vehiculos")]
[Authorize(Roles = Roles.Gestion)]
public class VehiculoController : ControladorBase
{
    private readonly IVehiculoLN _ln;

    public VehiculoController(IVehiculoLN ln) => _ln = ln;

    // ══════════════════ PUBLICO ══════════════════

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Listar(
        [FromQuery] FiltroVehiculoDto filtro, CancellationToken ct)
        => Resolver(await _ln.ListarPublicoAsync(filtro, ct));

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Detalle(string slug, CancellationToken ct)
        => Resolver(await _ln.BuscarPorSlugAsync(slug, ct));

    [HttpGet("destacado")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Destacado(CancellationToken ct)
        => Resolver(await _ln.DestacadoAsync(ct));

    // ══════════════════ PANEL ══════════════════

    [HttpGet("admin")]
    public async Task<IActionResult> ListarAdmin(
        [FromQuery] FiltroVehiculoDto filtro, CancellationToken ct)
        => Resolver(await _ln.ListarAdminAsync(filtro, ct));

    [HttpGet("admin/{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Resolver(await _ln.BuscarPorIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearVehiculoDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearAsync(dto, UsuarioActual, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarVehiculoDto dto, CancellationToken ct)
    {
        // El id de la ruta manda sobre el del cuerpo: sin esto se
        // podria editar un vehiculo distinto al de la URL.
        dto.Id = id;
        return Resolver(await _ln.ActualizarAsync(dto, UsuarioActual, ct));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Resolver(await _ln.CambiarEstadoAsync(dto, UsuarioActual, ct));
    }

    /// <summary>Eliminar es solo del propietario: es irreversible.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => Resolver(await _ln.EliminarAsync(id, UsuarioActual, ct));
}
