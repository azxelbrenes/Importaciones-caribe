using System.Security.Claims;
using Caribe.Utilitarios;
using Microsoft.AspNetCore.Mvc;

namespace Caribe.Api.Controllers;

/// <summary>
/// Base de todos los controladores.
///
/// Traduce Respuesta&lt;T&gt; al codigo HTTP correcto en un solo lugar:
/// sin esto, cada controlador repetiria el mismo switch y bastaria
/// que uno lo escribiera distinto para que el frontend recibiera
/// codigos inconsistentes.
/// </summary>
[ApiController]
public abstract class ControladorBase : ControllerBase
{
    /// <summary>Id del usuario autenticado. Vacio si no hay sesion.</summary>
    protected string UsuarioActual =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>
    /// IP del cliente.
    ///
    /// Detras de Caddy, RemoteIpAddress seria la del proxy, no la del
    /// visitante. Por eso se lee X-Forwarded-For primero, tomando la
    /// primera entrada: las siguientes son los proxies intermedios.
    /// </summary>
    protected string? IpCliente
    {
        get
        {
            var reenviada = Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(reenviada))
                return reenviada.Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    protected IActionResult Resolver<T>(Respuesta<T> r)
    {
        if (r.Exitoso) return Ok(r.Valor);

        var cuerpo = new { mensaje = r.Mensaje };

        return r.Error switch
        {
            TipoError.NoEncontrado => NotFound(cuerpo),
            TipoError.Validacion   => BadRequest(cuerpo),
            TipoError.Conflicto    => Conflict(cuerpo),
            TipoError.SinPermiso   => StatusCode(StatusCodes.Status403Forbidden, cuerpo),
            _                      => BadRequest(cuerpo)
        };
    }

    /// <summary>
    /// Para crear: devuelve 201 con el id en vez de 200.
    ///
    /// Importa mas de lo que parece: el frontend distingue "se creó"
    /// de "se actualizó" por el codigo, sin tener que inspeccionar el
    /// cuerpo.
    /// </summary>
    protected IActionResult ResolverCreado<T>(Respuesta<T> r)
    {
        if (!r.Exitoso) return Resolver(r);

        return StatusCode(StatusCodes.Status201Created, r.Valor);
    }
}
