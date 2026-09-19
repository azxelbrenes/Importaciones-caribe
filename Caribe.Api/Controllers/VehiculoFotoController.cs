using Caribe.AccesoDatos.Identidad;
using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caribe.Api.Controllers;

[Route("api/vehiculos/{vehiculoId:int}/fotos")]
[Authorize(Roles = Roles.Gestion)]
public class VehiculoFotoController : ControladorBase
{
    private readonly IFotoLN _ln;

    public VehiculoFotoController(IFotoLN ln) => _ln = ln;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar(int vehiculoId, CancellationToken ct)
        => Resolver(await _ln.ListarAsync(vehiculoId, ct));

    /// <summary>
    /// Subida multiple.
    ///
    /// Se procesa archivo por archivo y se devuelve el resultado de
    /// cada uno: si de ocho fotos una esta corrupta, las otras siete
    /// entran igual. Rechazar todo el lote por una obligaria a repetir
    /// la subida completa por datos moviles.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Subir(
        int vehiculoId, [FromForm] List<IFormFile> archivos, CancellationToken ct)
    {
        if (archivos is null || archivos.Count == 0)
            return BadRequest(new { mensaje = "No se recibió ningún archivo." });

        var subidas = new List<FotoSubidaDto>();
        var errores = new List<object>();

        foreach (var archivo in archivos)
        {
            await using var flujo = archivo.OpenReadStream();

            var r = await _ln.SubirAsync(
                vehiculoId, flujo, archivo.FileName, UsuarioActual, ct);

            if (r.Exitoso)
                subidas.Add(r.Valor!);
            else
                errores.Add(new { archivo = archivo.FileName, motivo = r.Mensaje });
        }

        return Ok(new { subidas, errores });
    }

    [HttpDelete("{fotoId:int}")]
    public async Task<IActionResult> Eliminar(int fotoId, CancellationToken ct)
        => Resolver(await _ln.EliminarAsync(fotoId, UsuarioActual, ct));

    [HttpPatch("{fotoId:int}/portada")]
    public async Task<IActionResult> MarcarPortada(int fotoId, CancellationToken ct)
        => Resolver(await _ln.MarcarPortadaAsync(fotoId, UsuarioActual, ct));

    [HttpPut("orden")]
    public async Task<IActionResult> Reordenar(
        int vehiculoId, [FromBody] ReordenarFotosDto dto, CancellationToken ct)
    {
        dto.VehiculoId = vehiculoId;
        return Resolver(await _ln.ReordenarAsync(dto, UsuarioActual, ct));
    }
}
