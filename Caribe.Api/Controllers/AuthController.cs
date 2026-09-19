using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControladorBase
{
    private const string CookieRefresh = "caribe_rt";

    private readonly IUsuarioLN _ln;

    public AuthController(IUsuarioLN ln) => _ln = ln;

    // ══════════════════ SESION ══════════════════

    [HttpPost("login")]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto, CancellationToken ct)
    {
        var r = await _ln.LoginAsync(dto, IpCliente, ct);

        if (!r.Exitoso) return Resolver(r);

        var (resultado, refresh) = r.Valor;

        // Falta el segundo factor: no hay sesion que crear todavia.
        if (resultado.RequiereDobleFactor) return Ok(resultado);

        if (refresh is not null) PonerCookie(refresh);

        return Ok(resultado);
    }

    [HttpPost("refrescar")]
    public async Task<IActionResult> Refrescar(CancellationToken ct)
    {
        var actual = Request.Cookies[CookieRefresh];

        if (string.IsNullOrEmpty(actual))
            return Unauthorized(new { mensaje = "No hay sesión activa." });

        var r = await _ln.RefrescarAsync(actual, IpCliente, ct);

        if (!r.Exitoso)
        {
            // La cookie se borra: si el token ya no sirve, dejarla
            // haria que el navegador reintente en cada carga.
            Response.Cookies.Delete(CookieRefresh, OpcionesCookie());
            return Unauthorized(new { mensaje = r.Mensaje });
        }

        PonerCookie(r.Valor.refresh);

        return Ok(r.Valor.token);
    }

    [HttpPost("cerrar-sesion")]
    [Authorize]
    public async Task<IActionResult> CerrarSesion(CancellationToken ct)
    {
        var actual = Request.Cookies[CookieRefresh];

        if (!string.IsNullOrEmpty(actual))
            await _ln.CerrarSesionAsync(actual, ct);

        Response.Cookies.Delete(CookieRefresh, OpcionesCookie());

        return Ok(new { mensaje = "Sesión cerrada." });
    }

    // ══════════════════ RECUPERACION ══════════════════

    /// <summary>
    /// Pide el enlace de recuperacion.
    ///
    /// Siempre responde Ok, exista o no la cuenta: responder distinto
    /// le confirmaria a un atacante quien tiene cuenta.
    /// </summary>
    [HttpPost("recuperar")]
    [EnableRateLimiting("recuperacion")]
    public async Task<IActionResult> Recuperar(
        [FromBody] SolicitarRecuperacionDto dto, CancellationToken ct)
    {
        await _ln.SolicitarRecuperacionAsync(dto, IpCliente, ct);

        return Ok(new
        {
            mensaje = "Si el correo está registrado, le enviamos un enlace " +
                      "para restablecer su contraseña."
        });
    }

    [HttpPost("restablecer")]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> Restablecer(
        [FromBody] RestablecerConTokenDto dto, CancellationToken ct)
        => Resolver(await _ln.RestablecerConTokenAsync(dto, ct));

    // ══════════════════ DOBLE FACTOR ══════════════════

    [HttpPost("doble-factor/configurar")]
    [Authorize]
    public async Task<IActionResult> ConfigurarDobleFactor(CancellationToken ct)
        => Resolver(await _ln.ConfigurarDobleFactorAsync(UsuarioActual, ct));

    [HttpPost("doble-factor/activar")]
    [Authorize]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> ActivarDobleFactor(
        [FromBody] ActivarDobleFactorDto dto, CancellationToken ct)
        => Resolver(await _ln.ActivarDobleFactorAsync(UsuarioActual, dto.Codigo, ct));

    [HttpPost("doble-factor/desactivar")]
    [Authorize]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> DesactivarDobleFactor(
        [FromBody] DesactivarDobleFactorDto dto, CancellationToken ct)
        => Resolver(await _ln.DesactivarDobleFactorAsync(UsuarioActual, dto.Password, ct));

    // ══════════════════ SESIONES ══════════════════

    [HttpGet("sesiones")]
    [Authorize]
    public async Task<IActionResult> Sesiones(CancellationToken ct)
        => Resolver(await _ln.ListarSesionesAsync(
            UsuarioActual, Request.Cookies[CookieRefresh], ct));

    [HttpPost("sesiones/cerrar-otras")]
    [Authorize]
    public async Task<IActionResult> CerrarOtrasSesiones(CancellationToken ct)
        => Resolver(await _ln.CerrarOtrasSesionesAsync(
            UsuarioActual, Request.Cookies[CookieRefresh], ct));

    // ══════════════════ INVITACION ══════════════════

    [HttpPost("aceptar-invitacion")]
    [EnableRateLimiting("formularios")]
    public async Task<IActionResult> AceptarInvitacion(
        [FromBody] AceptarInvitacionDto dto, CancellationToken ct)
        => Resolver(await _ln.AceptarInvitacionAsync(dto, ct));

    // ══════════════════ COOKIE ══════════════════

    private void PonerCookie(string valor)
        => Response.Cookies.Append(CookieRefresh, valor,
            OpcionesCookie(DateTimeOffset.UtcNow.AddDays(7)));

    /// <summary>
    /// HttpOnly: JavaScript no puede leerla, asi un XSS no roba la
    /// sesion. Es la razon por la que el access token vive en memoria
    /// y el refresh aqui.
    ///
    /// Secure: solo viaja por HTTPS.
    /// SameSite=Strict: no se envia desde otros sitios, corta CSRF.
    /// Path acotado: solo se manda a los endpoints que la necesitan.
    /// </summary>
    private static CookieOptions OpcionesCookie(DateTimeOffset? expira = null) => new()
    {
        HttpOnly = true,
        Secure   = true,
        SameSite = SameSiteMode.Strict,
        Path     = "/api/auth",
        Expires  = expira
    };
}
