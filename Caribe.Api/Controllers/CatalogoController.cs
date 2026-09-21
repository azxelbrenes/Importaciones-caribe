using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Catalogos;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

[Route("api/catalogo")]
[Authorize(Roles = Roles.Gestion)]
public class CatalogoController : ControladorBase
{
    private readonly ICatalogoLN _ln;

    public CatalogoController(ICatalogoLN ln) => _ln = ln;

    // ══════════════════ PUBLICO ══════════════════

    [HttpGet("marcas")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Marcas(
        [FromQuery] bool soloActivas = true, CancellationToken ct = default)
        => Resolver(await _ln.ListarMarcasAsync(soloActivas, ct));

    [HttpGet("modelos")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Modelos(
        [FromQuery] int? marcaId, CancellationToken ct)
        => Resolver(await _ln.ListarModelosAsync(marcaId, ct));

    /// <summary>
    /// Opciones de los desplegables. Publico porque el formulario del
    /// sitio las necesita para armar sus selectores.
    /// </summary>
    [HttpGet("opciones/{tipo}")]
    [AllowAnonymous]
    [EnableRateLimiting("general")]
    public IActionResult Opciones(string tipo)
        => Resolver(_ln.ListarOpciones(tipo));

    // ══════════════════ CREAR ══════════════════

    [HttpPost("marcas")]
    public async Task<IActionResult> CrearMarca(
        [FromBody] CrearMarcaDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearMarcaAsync(dto, UsuarioActual, ct));

    [HttpPost("modelos")]
    public async Task<IActionResult> CrearModelo(
        [FromBody] CrearModeloDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearModeloAsync(dto, UsuarioActual, ct));

    // ══════════════════ DESACTIVAR ══════════════════

    [HttpPatch("marcas/{id:int}/estado")]
    public async Task<IActionResult> ActivarMarca(
        int id, [FromQuery] bool activa, CancellationToken ct)
        => Resolver(await _ln.ActivarMarcaAsync(id, activa, UsuarioActual, ct));

    [HttpPatch("modelos/{id:int}/estado")]
    public async Task<IActionResult> ActivarModelo(
        int id, [FromQuery] bool activo, CancellationToken ct)
        => Resolver(await _ln.ActivarModeloAsync(id, activo, UsuarioActual, ct));

    // ══════════════════ ELIMINAR ══════════════════
    // Solo el propietario: borrar del catalogo es irreversible y
    // puede afectar el historial si algo sale mal.

    [HttpDelete("marcas/{id:int}")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> EliminarMarca(int id, CancellationToken ct)
        => Resolver(await _ln.EliminarMarcaAsync(id, UsuarioActual, ct));

    [HttpDelete("modelos/{id:int}")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> EliminarModelo(int id, CancellationToken ct)
        => Resolver(await _ln.EliminarModeloAsync(id, UsuarioActual, ct));

    /// <summary>
    /// Limpieza de duplicados. Se ejecuta una sola vez y toca muchos
    /// registros a la vez, asi que es solo del propietario.
    /// </summary>
    [HttpPost("limpiar-duplicados")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> LimpiarDuplicados(CancellationToken ct)
        => Resolver(await _ln.LimpiarDuplicadosAsync(UsuarioActual, ct));
}
