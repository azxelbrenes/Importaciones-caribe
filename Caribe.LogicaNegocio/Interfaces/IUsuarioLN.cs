using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Interfaces;

public interface IUsuarioLN
{
    // ══════════════════ AUTENTICACION ══════════════════

    /// <summary>
    /// Login completo. Devuelve el resultado y el refresh token en
    /// claro para que el controlador lo ponga en la cookie: la logica
    /// de negocio no conoce HTTP.
    /// </summary>
    Task<Respuesta<(LoginResultadoDto resultado, string? refresh)>> LoginAsync(
        LoginDto dto, string? ip, CancellationToken ct = default);

    Task<Respuesta<(TokenDto token, string refresh)>> RefrescarAsync(
        string refreshEnClaro, string? ip, CancellationToken ct = default);

    Task<Respuesta<bool>> CerrarSesionAsync(
        string refreshEnClaro, CancellationToken ct = default);

    // ══════════════════ RECUPERACION ══════════════════

    /// <summary>
    /// Envia el enlace de recuperacion.
    ///
    /// Devuelve Ok aunque el correo no exista: decir "ese correo no
    /// esta registrado" le confirmaria a un atacante quien tiene
    /// cuenta.
    /// </summary>
    Task<Respuesta<bool>> SolicitarRecuperacionAsync(
        SolicitarRecuperacionDto dto, string? ip, CancellationToken ct = default);

    Task<Respuesta<bool>> RestablecerConTokenAsync(
        RestablecerConTokenDto dto, CancellationToken ct = default);

    /// <summary>
    /// El propietario le genera una contrasena temporal a otro
    /// usuario. Via alternativa cuando el correo no esta disponible.
    /// </summary>
    Task<Respuesta<PasswordRestablecidaDto>> RestablecerPasswordAsync(
        string usuarioId, string ejecutorId, CancellationToken ct = default);

    // ══════════════════ DOBLE FACTOR ══════════════════

    Task<Respuesta<ConfigurarDobleFactorDto>> ConfigurarDobleFactorAsync(
        string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> ActivarDobleFactorAsync(
        string usuarioId, string codigo, CancellationToken ct = default);

    Task<Respuesta<bool>> DesactivarDobleFactorAsync(
        string usuarioId, string password, CancellationToken ct = default);

    // ══════════════════ SESIONES ══════════════════

    Task<Respuesta<IEnumerable<SesionDto>>> ListarSesionesAsync(
        string usuarioId, string? refreshActual, CancellationToken ct = default);

    Task<Respuesta<int>> CerrarOtrasSesionesAsync(
        string usuarioId, string? refreshActual, CancellationToken ct = default);

    // ══════════════════ GESTION ══════════════════

    Task<Respuesta<IEnumerable<UsuarioDto>>> ListarAsync(
        CancellationToken ct = default);

    Task<Respuesta<bool>> CambiarPasswordAsync(
        CambiarPasswordDto dto, string usuarioId, CancellationToken ct = default);

    Task<Respuesta<bool>> ActivarDesactivarAsync(
        string id, bool activo, string ejecutorId, CancellationToken ct = default);

    // ══════════════════ INVITACIONES ══════════════════

    Task<Respuesta<InvitacionCreadaDto>> InvitarAsync(
        CrearInvitacionDto dto, string invitadoPorId, CancellationToken ct = default);

    Task<Respuesta<bool>> AceptarInvitacionAsync(
        AceptarInvitacionDto dto, CancellationToken ct = default);

    Task<Respuesta<IEnumerable<InvitacionDto>>> ListarInvitacionesAsync(
        CancellationToken ct = default);

    Task<Respuesta<bool>> RevocarInvitacionAsync(
        int id, string usuarioId, CancellationToken ct = default);
}
