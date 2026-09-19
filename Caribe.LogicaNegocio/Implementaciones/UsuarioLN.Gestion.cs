using Caribe.AccesoDatos.Identidad;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Correo;
using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.LogicaNegocio.Seguridad;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

/// <summary>
/// Recuperacion de contrasena, gestion de cuentas e invitaciones.
/// </summary>
public partial class UsuarioLN
{
    // ══════════════════ RECUPERACION ══════════════════

    /// <summary>
    /// Envia el enlace de recuperacion al correo.
    ///
    /// SIEMPRE devuelve Ok, exista o no la cuenta. Si respondiera
    /// distinto, cualquiera podria averiguar quien tiene cuenta
    /// probando direcciones — y el panel de un negocio que maneja
    /// depositos es justo donde eso importa.
    /// </summary>
    public async Task<Respuesta<bool>> SolicitarRecuperacionAsync(
        SolicitarRecuperacionDto dto, string? ip, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var u = await _users.FindByEmailAsync(email);

        // Se sale en silencio: para quien lo pidio, el resultado se ve
        // igual que si hubiera funcionado.
        if (u is null || !u.Activo)
            return Respuesta<bool>.Ok(true);

        var ahora = DateTimeOffset.UtcNow;
        var haceUnaHora = ahora.AddHours(-1);

        // Freno al abuso: tres enlaces por hora. Sin esto, alguien
        // podria llenarle el buzon a otra persona pidiendo
        // recuperaciones en bucle.
        var recientes = await _db.TokensRecuperacion
            .CountAsync(t => t.UsuarioId == u.Id && t.CreadoEn > haceUnaHora, ct);

        if (recientes >= 3)
            return Respuesta<bool>.Ok(true);

        // Los pendientes anteriores se invalidan: si se piden tres
        // enlaces, solo el ultimo debe servir.
        await _db.TokensRecuperacion
            .Where(t => t.UsuarioId == u.Id && t.UsadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora), ct);

        var enClaro = Hash.GenerarToken();

        _db.TokensRecuperacion.Add(new TokenRecuperacion
        {
            UsuarioId = u.Id,
            TokenHash = Hash.Calcular(enClaro),
            ExpiraEn = ahora.AddMinutes(_op.MinutosTokenRecuperacion),
            CreadoEn = ahora,
            IpSolicitud = ip
        });

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, u.Id,
            despues: new { Accion = "Solicitud de recuperación de contraseña" }, ip: ip);

        await _db.SaveChangesAsync(ct);

        var enlace = $"{_correoOp.UrlSitio.TrimEnd('/')}/restablecer?token={enClaro}";

        await _correo.EnviarAsync(
            email,
            "Restablecer su contraseña",
            Plantillas.RecuperarPassword(
                enlace, u.NombreCompleto, _op.MinutosTokenRecuperacion),
            ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> RestablecerConTokenAsync(
        RestablecerConTokenDto dto, CancellationToken ct = default)
    {
        if (dto.PasswordNueva != dto.ConfirmarPassword)
            return Respuesta<bool>.Invalido("Las contraseñas no coinciden.");

        var hash = Hash.Calcular(dto.Token);
        var ahora = DateTimeOffset.UtcNow;

        var token = await _db.TokensRecuperacion
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.EsValido(ahora))
            return Respuesta<bool>.Invalido(
                "El enlace no es válido o ya venció. Solicite uno nuevo.");

        var u = await _users.FindByIdAsync(token.UsuarioId);

        if (u is null || !u.Activo)
            return Respuesta<bool>.Invalido("La cuenta ya no está activa.");

        var resetToken = await _users.GeneratePasswordResetTokenAsync(u);
        var r = await _users.ResetPasswordAsync(u, resetToken, dto.PasswordNueva);

        if (!r.Succeeded)
            return Respuesta<bool>.Invalido(
                string.Join(" ", r.Errors.Select(e => e.Description)));

        // Uso unico: se marca antes de cualquier otra cosa para que el
        // mismo enlace no sirva dos veces.
        token.UsadoEn = ahora;

        // Todas las sesiones se cierran. Si la contrasena se
        // restablece porque alguien mas entro, dejar sesiones vivas
        // haria inutil el cambio.
        await _db.RefreshTokens
            .Where(t => t.UsuarioId == u.Id && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);

        // Y se levanta el bloqueo: si quedo bloqueada por intentos
        // fallidos al olvidar la clave, con la nueva debe poder entrar
        // de inmediato.
        await _users.SetLockoutEndDateAsync(u, null);
        await _users.ResetAccessFailedCountAsync(u);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, u.Id,
            despues: new { Accion = "Contraseña restablecida con enlace" });

        await _db.SaveChangesAsync(ct);

        // Aviso al correo. Si la persona no hizo el cambio, esto es lo
        // que le permite darse cuenta.
        await _correo.EnviarAsync(
            u.Email ?? string.Empty,
            "Su contraseña cambió",
            Plantillas.PasswordCambiada(u.NombreCompleto),
            ct);

        return Respuesta<bool>.Ok(true);
    }

    /// <summary>
    /// El propietario genera una contrasena temporal para otro
    /// usuario. Via alternativa cuando el correo no esta disponible.
    ///
    /// Devolver la contrasena es aceptable aqui: quien la pide es un
    /// SuperAdministrador autenticado que ya tiene control total del
    /// sistema, asi que no le agrega ningun poder que no tuviera.
    /// </summary>
    public async Task<Respuesta<PasswordRestablecidaDto>> RestablecerPasswordAsync(
        string usuarioId, string ejecutorId, CancellationToken ct = default)
    {
        if (usuarioId == ejecutorId)
            return Respuesta<PasswordRestablecidaDto>.Conflicto(
                "Para cambiar tu propia contraseña usá Mi cuenta.");

        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<PasswordRestablecidaDto>.NoEncontrado("El usuario no existe.");

        var temporal = Hash.GenerarPasswordTemporal();

        var token = await _users.GeneratePasswordResetTokenAsync(u);
        var r = await _users.ResetPasswordAsync(u, token, temporal);

        if (!r.Succeeded)
            return Respuesta<PasswordRestablecidaDto>.Invalido(
                string.Join(" ", r.Errors.Select(e => e.Description)));

        var ahora = DateTimeOffset.UtcNow;

        await _db.RefreshTokens
            .Where(t => t.UsuarioId == u.Id && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);

        await _users.SetLockoutEndDateAsync(u, null);
        await _users.ResetAccessFailedCountAsync(u);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, ejecutorId,
            despues: new { Accion = "Contraseña restablecida por el propietario" });

        await _db.SaveChangesAsync(ct);

        return Respuesta<PasswordRestablecidaDto>.Ok(
            new PasswordRestablecidaDto(u.Email ?? "", temporal));
    }

    // ══════════════════ GESTION ══════════════════

    public async Task<Respuesta<IEnumerable<UsuarioDto>>> ListarAsync(
        CancellationToken ct = default)
    {
        var usuarios = await _users.Users.AsNoTracking().ToListAsync(ct);
        var lista = new List<UsuarioDto>();

        foreach (var u in usuarios)
        {
            lista.Add(new UsuarioDto(
                u.Id, u.NombreCompleto, u.Email ?? "",
                await _users.GetRolesAsync(u),
                u.Activo, u.TwoFactorEnabled, u.UltimoAcceso, u.CreadoEn));
        }

        return Respuesta<IEnumerable<UsuarioDto>>.Ok(lista);
    }

    public async Task<Respuesta<bool>> CambiarPasswordAsync(
        CambiarPasswordDto dto, string usuarioId, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<bool>.NoEncontrado("El usuario no existe.");

        var r = await _users.ChangePasswordAsync(
            u, dto.PasswordActual, dto.PasswordNueva);

        if (!r.Succeeded)
            return Respuesta<bool>.Invalido(
                string.Join(" ", r.Errors.Select(e => e.Description)));

        await _db.RefreshTokens
            .Where(t => t.UsuarioId == u.Id && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(
                t => t.RevocadoEn, DateTimeOffset.UtcNow), ct);

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Editar, usuarioId,
            despues: new { Accion = "Cambio de contraseña" });

        await _db.SaveChangesAsync(ct);

        await _correo.EnviarAsync(
            u.Email ?? string.Empty,
            "Su contraseña cambió",
            Plantillas.PasswordCambiada(u.NombreCompleto),
            ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> ActivarDesactivarAsync(
        string id, bool activo, string ejecutorId, CancellationToken ct = default)
    {
        // Nadie puede desactivarse a si mismo: quedaria fuera de su
        // propio panel sin forma de volver.
        if (id == ejecutorId)
            return Respuesta<bool>.Conflicto("No puede desactivar su propia cuenta.");

        var u = await _users.FindByIdAsync(id);

        if (u is null)
            return Respuesta<bool>.NoEncontrado("El usuario no existe.");

        u.Activo = activo;
        await _users.UpdateAsync(u);

        // Al desactivar se cierran sus sesiones de inmediato. Sin
        // esto seguiria dentro hasta que venciera su refresh token:
        // siete dias.
        if (!activo)
        {
            await _db.RefreshTokens
                .Where(t => t.UsuarioId == id && t.RevocadoEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    t => t.RevocadoEn, DateTimeOffset.UtcNow), ct);
        }

        _db.Registrar("Usuario", id, AccionAuditoria.Editar, ejecutorId,
            despues: new { Activo = activo });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    // ══════════════════ INVITACIONES ══════════════════

    public async Task<Respuesta<InvitacionCreadaDto>> InvitarAsync(
        CrearInvitacionDto dto, string invitadoPorId, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _users.FindByEmailAsync(email) is not null)
            return Respuesta<InvitacionCreadaDto>
                .Conflicto("Ya existe una cuenta con ese correo.");

        var ahora = DateTimeOffset.UtcNow;

        var pendiente = await _db.Invitaciones.AnyAsync(i =>
            i.Email == email && i.UsadaEn == null
            && i.RevocadaEn == null && i.ExpiraEn > ahora, ct);

        if (pendiente)
            return Respuesta<InvitacionCreadaDto>
                .Conflicto("Ya hay una invitación pendiente para ese correo.");

        var enClaro = Hash.GenerarToken();

        var inv = new Invitacion
        {
            Email = email,
            Rol = (RolInvitacion)dto.Rol,
            TokenHash = Hash.Calcular(enClaro),
            InvitadoPorId = invitadoPorId,
            ExpiraEn = ahora.AddHours(72),
            CreadoEn = ahora
        };

        _db.Invitaciones.Add(inv);

        _db.Registrar("Invitacion", 0, AccionAuditoria.Crear, invitadoPorId,
            despues: new { inv.Email, Rol = Etiquetas.De(inv.Rol) });

        await _db.SaveChangesAsync(ct);

        // Se envia DESPUES de guardar: si el correo falla, la
        // invitacion ya existe y se puede reenviar. Al reves, un fallo
        // de red dejaria un correo circulando con un token que no esta
        // en la base.
        var invitador = await _users.FindByIdAsync(invitadoPorId);
        var enlace = $"{_correoOp.UrlSitio.TrimEnd('/')}/aceptar-invitacion?token={enClaro}";

        var enviado = await _correo.EnviarAsync(
            email,
            "Le invitaron al panel de Importaciones del Caribe CR",
            Plantillas.Invitacion(
                enlace,
                Etiquetas.De(inv.Rol),
                invitador?.NombreCompleto ?? "El administrador"),
            ct);

        // El enlace se devuelve solo si el correo NO salio de verdad.
        // Cuando Resend este activo, EnvioReal es true y el panel deja
        // de mostrarlo sin tocar el frontend.
        var salioDeVerdad = enviado && _correo.EnvioReal;

        return Respuesta<InvitacionCreadaDto>.Ok(new InvitacionCreadaDto(
            inv.Id, email, salioDeVerdad, salioDeVerdad ? null : enlace));
    }

    public async Task<Respuesta<bool>> AceptarInvitacionAsync(
        AceptarInvitacionDto dto, CancellationToken ct = default)
    {
        if (dto.Password != dto.ConfirmarPassword)
            return Respuesta<bool>.Invalido("Las contraseñas no coinciden.");

        var hash = Hash.Calcular(dto.Token);
        var ahora = DateTimeOffset.UtcNow;

        var inv = await _db.Invitaciones
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);

        if (inv is null || !inv.EsValida(ahora))
            return Respuesta<bool>.Invalido("La invitación no es válida o ya venció.");

        if (await _users.FindByEmailAsync(inv.Email) is not null)
            return Respuesta<bool>.Conflicto("Ya existe una cuenta con ese correo.");

        var u = new AppUser
        {
            UserName = inv.Email,
            Email = inv.Email,
            // El correo se probo al abrir el enlace: solo quien tiene
            // acceso a ese buzon pudo llegar hasta aqui.
            EmailConfirmed = true,
            NombreCompleto = dto.NombreCompleto.Trim(),
            InvitadoPorId = inv.InvitadoPorId,
            Activo = true,
            CreadoEn = ahora
        };

        var r = await _users.CreateAsync(u, dto.Password);

        if (!r.Succeeded)
            return Respuesta<bool>.Invalido(
                string.Join(" ", r.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(u, inv.Rol == RolInvitacion.Administrador
            ? Roles.Administrador
            : Roles.Operador);

        inv.UsadaEn = ahora;

        _db.Registrar("Usuario", u.Id, AccionAuditoria.Crear, inv.InvitadoPorId,
            despues: new { u.Email, Rol = Etiquetas.De(inv.Rol) });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<IEnumerable<InvitacionDto>>> ListarInvitacionesAsync(
        CancellationToken ct = default)
    {
        var ahora = DateTimeOffset.UtcNow;

        // Nunca se devuelve el token ni su hash.
        var lista = await _db.Invitaciones
            .AsNoTracking()
            .OrderByDescending(i => i.CreadoEn)
            .Select(i => new InvitacionDto(
                i.Id,
                i.Email,
                (short)i.Rol,
                Etiquetas.De(i.Rol),
                i.InvitadoPorId,
                i.ExpiraEn,
                i.UsadaEn != null,
                i.RevocadaEn != null,
                i.UsadaEn == null && i.RevocadaEn == null && i.ExpiraEn > ahora,
                i.CreadoEn))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<InvitacionDto>>.Ok(lista);
    }

    public async Task<Respuesta<bool>> RevocarInvitacionAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var inv = await _db.Invitaciones.FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inv is null)
            return Respuesta<bool>.NoEncontrado("La invitación no existe.");

        if (inv.UsadaEn is not null)
            return Respuesta<bool>.Conflicto("La invitación ya fue utilizada.");

        inv.RevocadaEn = DateTimeOffset.UtcNow;

        _db.Registrar("Invitacion", inv.Id, AccionAuditoria.Eliminar, usuarioId,
            antes: new { inv.Email });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }
}
