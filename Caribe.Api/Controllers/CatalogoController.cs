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

    [HttpPost("marcas")]
    public async Task<IActionResult> CrearMarca(
        [FromBody] CrearMarcaDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearMarcaAsync(dto, UsuarioActual, ct));

    [HttpPost("modelos")]
    public async Task<IActionResult> CrearModelo(
        [FromBody] CrearModeloDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearModeloAsync(dto, UsuarioActual, ct));
}
