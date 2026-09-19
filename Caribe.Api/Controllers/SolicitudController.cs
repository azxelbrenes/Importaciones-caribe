using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Solicitudes;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

/// <summary>
/// Atencion incluye al Operador: la persona que atiende solicitudes
/// no necesita ver vehiculos ni margenes.
/// </summary>
[Route("api/solicitudes")]
[Authorize(Roles = Roles.Atencion)]
public class SolicitudController : ControladorBase
{
    private readonly ISolicitudLN _ln;

    public SolicitudController(ISolicitudLN ln) => _ln = ln;

    /// <summary>
    /// Formulario publico. Con limite estricto: es el unico endpoint
    /// que escribe en la base sin autenticacion.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearSolicitudDto dto, CancellationToken ct)
        => ResolverCreado(await _ln.CrearAsync(dto, IpCliente, ct));

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] FiltroSolicitudDto filtro, CancellationToken ct)
        => Resolver(await _ln.ListarAsync(filtro, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
        => Resolver(await _ln.BuscarPorIdAsync(id, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarSolicitudDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Resolver(await _ln.ActualizarAsync(dto, UsuarioActual, ct));
    }

    [HttpPost("{id:int}/notas")]
    public async Task<IActionResult> AgregarNota(
        int id, [FromBody] CrearNotaDto dto, CancellationToken ct)
    {
        dto.SolicitudId = id;
        return ResolverCreado(await _ln.AgregarNotaAsync(dto, UsuarioActual, ct));
    }

    // ══════════════════ LIMPIEZA ══════════════════

    [HttpPost("{id:int}/archivar")]
    public async Task<IActionResult> Archivar(int id, CancellationToken ct)
        => Resolver(await _ln.ArchivarAsync(id, UsuarioActual, ct));

    /// <summary>
    /// Archivado en lote. Solo Gestion: afecta muchos registros y no
    /// es una tarea del dia a dia.
    /// </summary>
    [HttpPost("archivar-cerradas")]
    [Authorize(Roles = Roles.Gestion)]
    public async Task<IActionResult> ArchivarCerradas(
        [FromQuery] int meses = 6, CancellationToken ct = default)
        => Resolver(await _ln.ArchivarCerradasAsync(meses, UsuarioActual, ct));

    /// <summary>
    /// Borrado real. Solo el propietario: es irreversible y afecta las
    /// estadisticas del negocio.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.SuperAdministrador)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
        => Resolver(await _ln.EliminarAsync(id, UsuarioActual, ct));
}
