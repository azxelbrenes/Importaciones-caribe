using System.Text.Encodings.Web;
using Caribe.AccesoDatos.Contexto;
using Caribe.AccesoDatos.Identidad;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Correo;
using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.LogicaNegocio.Seguridad;
using Caribe.Utilitarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Caribe.LogicaNegocio.Implementaciones;

/// <summary>
/// Autenticacion, doble factor y sesiones.
///
/// La clase se parte en dos archivos porque junta tres
/// responsabilidades que crecen distinto: autenticacion aqui, y
/// gestion de cuentas e invitaciones en el otro. Es la misma clase,
/// solo separada para poder leerla.
/// </summary>
public partial class UsuarioLN : IUsuarioLN
{
    private readonly CaribeContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IJwtService _jwt;
    private readonly JwtOpciones _op;
    private readonly ICorreoService _correo;
    private readonly CorreoOpciones _correoOp;

    public UsuarioLN(
        CaribeContext db,
        UserManager<AppUser> users,
        IJwtService jwt,
        IOptions<JwtOpciones> op,
        ICorreoService correo,
        IOptions<CorreoOpciones> correoOp)
        => (_db, _users, _jwt, _op, _correo, _correoOp)
           = (db, users, jwt, op.Value, correo, correoOp.Value);

    // ══════════════════ LOGIN ══════════════════

    public async Task<Respuesta<(LoginResultadoDto resultado, string? refresh)>> LoginAsync(
        LoginDto dto, string? ip, CancellationToken ct = default)
    {
        var u = await _users.FindByEmailAsync(dto.Email);

        // Mismo mensaje exista o no el usuario. Si dijera "ese correo
        // no esta registrado", cualquiera podria averiguar quien tiene
        // cuenta probando direcciones.
        const string generico = "Correo o contraseña incorrectos.";

        if (u is null)
            return Respuesta<(LoginResultadoDto, string?)>.Invalido(generico);

        if (!u.Activo)
            return Respuesta<(LoginResultadoDto, string?)>
                .SinPermiso("La cuenta está desactivada.");

        if (await _users.IsLockedOutAsync(u))
            return Respuesta<(LoginResultadoDto, string?)>.SinPermiso(
                "Cuenta bloqueada temporalmente por intentos fallidos. " +
                "Intente en 15 minutos.");

        if (!await _users.CheckPasswordAsync(u, dto.Password))
        {
            await _users.AccessFailedAsync(u);
            return Respuesta<(LoginResultadoDto, string?)>.Invalido(generico);
        }

        // ── Segundo factor ──
        if (await _users.GetTwoFactorEnabledAsync(u))
        {
            if (string.IsNullOrWhiteSpace(dto.CodigoDobleFactor))
            {
                // Contrasena correcta, falta el codigo. NO se resetea
                // el contador de fallos todavia: la sesion aun no esta
                // abierta.
                return Respuesta<(LoginResultadoDto, string?)>.Ok(
                    (new LoginResultadoDto(true, null), null));
            }

            var valido = await _users.VerifyTwoFactorTokenAsync(
                u,
                _users.Options.Tokens.AuthenticatorTokenProvider,
                dto.CodigoDobleFactor.Replace(" ", "").Trim());

            if (!valido)
            {
                // El codigo fallido tambien cuenta para el bloqueo. Sin
                // esto, alguien con la contrasena podria probar codigos
                // de seis digitos sin limite hasta acertar.
                await _users.AccessFailedAsync(u);

                _db.Registrar("Usuario", u.Id, AccionAuditoria.Acceso, u.Id,
                    despues: new { Resultado = "Código de verificación incorrecto" },
                    ip: ip);

                await _db.SaveChangesAsync(ct);

                return Respuesta<(LoginResultadoDto, string?)>
                    .Invalido("El código de verificación no es correcto.");
            }
        }

        await _users.ResetAccessFailedCountAsync(u);

        var roles = await _users.GetRolesAsync(u);
        var (token, expira) = _jwt.CrearAccessToken(u, roles);

        u.UltimoAcceso = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(u);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Acceso, u.Id,
            despues: new { Resultado = "Acceso correcto" }, ip: ip);

        await _db.SaveChangesAsync(ct);

        var refresh = await CrearRefreshTokenAsync(u.Id, ip, ct);

        var dtoToken = new TokenDto(
            token, expira, u.NombreCompleto, u.Email ?? string.Empty, roles);

        return Respuesta<(LoginResultadoDto, string?)>.Ok(
            (new LoginResultadoDto(false, dtoToken), refresh));
    }

    /// <summary>
    /// Rotacion: cada uso invalida el anterior y emite uno nuevo.
    ///
    /// Si llega un token ya usado, se revocan TODAS las sesiones del
    /// usuario. Un token usado dos veces significa que alguien mas
    /// tiene una copia, y no hay forma de saber cual de los dos es el
    /// legitimo: lo seguro es expulsar a ambos.
    /// </summary>
    public async Task<Respuesta<(TokenDto token, string refresh)>> RefrescarAsync(
        string refreshEnClaro, string? ip, CancellationToken ct = default)
    {
        var hash = Hash.Calcular(refreshEnClaro);
        var ahora = DateTimeOffset.UtcNow;

        var guardado = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (guardado is null)
            return Respuesta<(TokenDto, string)>.SinPermiso("Sesión no válida.");

        if (guardado.RevocadoEn is not null)
        {
            await _db.RefreshTokens
                .Where(t => t.UsuarioId == guardado.UsuarioId && t.RevocadoEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);

            _db.Registrar("Usuario", guardado.UsuarioId, AccionAuditoria.Acceso,
                guardado.UsuarioId,
                despues: new { Alerta = "Reutilización de refresh token detectada" },
                ip: ip);

            await _db.SaveChangesAsync(ct);

            return Respuesta<(TokenDto, string)>.SinPermiso(
                "Sesión comprometida. Vuelva a iniciar sesión.");
        }

        if (!guardado.EstaActivo(ahora))
            return Respuesta<(TokenDto, string)>.SinPermiso("La sesión expiró.");

        var u = await _users.FindByIdAsync(guardado.UsuarioId);

        if (u is null || !u.Activo)
            return Respuesta<(TokenDto, string)>.SinPermiso("La cuenta ya no está activa.");

        var nuevoEnClaro = Hash.GenerarToken();
        var nuevoHash = Hash.Calcular(nuevoEnClaro);

        guardado.RevocadoEn = ahora;
        guardado.ReemplazadoPor = nuevoHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = u.Id,
            TokenHash = nuevoHash,
            ExpiraEn = ahora.AddDays(_op.DiasRefreshToken),
            CreadoEn = ahora,
            Ip = ip
        });

        await _db.SaveChangesAsync(ct);

        var roles = await _users.GetRolesAsync(u);
        var (token, expira) = _jwt.CrearAccessToken(u, roles);

        return Respuesta<(TokenDto, string)>.Ok((
            new TokenDto(token, expira, u.NombreCompleto, u.Email ?? "", roles),
            nuevoEnClaro));
    }

    public async Task<Respuesta<bool>> CerrarSesionAsync(
        string refreshEnClaro, CancellationToken ct = default)
    {
        var hash = Hash.Calcular(refreshEnClaro);

        await _db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(
                t => t.RevocadoEn, DateTimeOffset.UtcNow), ct);

        return Respuesta<bool>.Ok(true);
    }

    // ══════════════════ DOBLE FACTOR ══════════════════

    public async Task<Respuesta<ConfigurarDobleFactorDto>> ConfigurarDobleFactorAsync(
        string usuarioId, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<ConfigurarDobleFactorDto>.NoEncontrado("El usuario no existe.");

        if (await _users.GetTwoFactorEnabledAsync(u))
            return Respuesta<ConfigurarDobleFactorDto>
                .Conflicto("La verificación en dos pasos ya está activa.");

        // Clave nueva cada vez que se entra a configurar: si alguien
        // abandono el proceso a medias, la anterior queda inservible
        // en lugar de quedar flotando activa.
        await _users.ResetAuthenticatorKeyAsync(u);
        var clave = await _users.GetAuthenticatorKeyAsync(u);

        if (string.IsNullOrEmpty(clave))
            return Respuesta<ConfigurarDobleFactorDto>
                .Invalido("No se pudo generar la clave de verificación.");

        const string emisor = "Importaciones del Caribe CR";

        var uri = string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            UrlEncoder.Default.Encode(emisor),
            UrlEncoder.Default.Encode(u.Email ?? u.UserName ?? "usuario"),
            clave);

        return Respuesta<ConfigurarDobleFactorDto>.Ok(
            new ConfigurarDobleFactorDto(FormatearClave(clave), uri));
    }

    /// <summary>
    /// Se exige el codigo ANTES de activar.
    ///
    /// Si se activara sin confirmar, alguien que configuro mal la
    /// aplicacion quedaria bloqueado fuera de su propia cuenta sin
    /// forma de entrar.
    /// </summary>
    public async Task<Respuesta<bool>> ActivarDobleFactorAsync(
        string usuarioId, string codigo, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<bool>.NoEncontrado("El usuario no existe.");

        var valido = await _users.VerifyTwoFactorTokenAsync(
            u,
            _users.Options.Tokens.AuthenticatorTokenProvider,
            codigo.Replace(" ", "").Trim());

        if (!valido)
            return Respuesta<bool>.Invalido(
                "El código no es correcto. Verifique que la hora del " +
                "teléfono esté sincronizada.");

        await _users.SetTwoFactorEnabledAsync(u, true);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, usuarioId,
            despues: new { DobleFactor = "Activado" });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> DesactivarDobleFactorAsync(
        string usuarioId, string password, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<bool>.NoEncontrado("El usuario no existe.");

        if (!await _users.CheckPasswordAsync(u, password))
            return Respuesta<bool>.Invalido("La contraseña no es correcta.");

        await _users.SetTwoFactorEnabledAsync(u, false);
        await _users.ResetAuthenticatorKeyAsync(u);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, usuarioId,
            despues: new { DobleFactor = "Desactivado" });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    // ══════════════════ SESIONES ══════════════════

    public async Task<Respuesta<IEnumerable<SesionDto>>> ListarSesionesAsync(
        string usuarioId, string? refreshActual, CancellationToken ct = default)
    {
        var ahora = DateTimeOffset.UtcNow;

        var hashActual = string.IsNullOrEmpty(refreshActual)
            ? null
            : Hash.Calcular(refreshActual);

        var lista = await _db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UsuarioId == usuarioId
                     && t.RevocadoEn == null
                     && t.ExpiraEn > ahora)
            .OrderByDescending(t => t.CreadoEn)
            .Select(t => new SesionDto(
                t.Id, t.Ip, t.CreadoEn, t.ExpiraEn,
                hashActual != null && t.TokenHash == hashActual))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<SesionDto>>.Ok(lista);
    }

    public async Task<Respuesta<int>> CerrarOtrasSesionesAsync(
        string usuarioId, string? refreshActual, CancellationToken ct = default)
    {
        var ahora = DateTimeOffset.UtcNow;

        var hashActual = string.IsNullOrEmpty(refreshActual)
            ? null
            : Hash.Calcular(refreshActual);

        var cerradas = await _db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId
                     && t.RevocadoEn == null
                     && (hashActual == null || t.TokenHash != hashActual))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);

        _db.Registrar("Usuario", usuarioId, AccionAuditoria.Editar, usuarioId,
            despues: new { Accion = "Cierre de otras sesiones", Cantidad = cerradas });

        await _db.SaveChangesAsync(ct);

        return Respuesta<int>.Ok(cerradas);
    }

    // ══════════════════ PRIVADOS ══════════════════

    private async Task<string> CrearRefreshTokenAsync(
        string usuarioId, string? ip, CancellationToken ct)
    {
        var enClaro = Hash.GenerarToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuarioId,
            TokenHash = Hash.Calcular(enClaro),
            ExpiraEn = DateTimeOffset.UtcNow.AddDays(_op.DiasRefreshToken),
            CreadoEn = DateTimeOffset.UtcNow,
            Ip = ip
        });

        await _db.SaveChangesAsync(ct);

        return enClaro;
    }

    /// <summary>Agrupa la clave de 4 en 4 para copiarla sin errores.</summary>
    private static string FormatearClave(string clave)
    {
        var sb = new System.Text.StringBuilder();

        for (var i = 0; i < clave.Length; i += 4)
            sb.Append(clave.AsSpan(i, Math.Min(4, clave.Length - i))).Append(' ');

        return sb.ToString().Trim().ToUpperInvariant();
    }
}
