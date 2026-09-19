using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Usuarios;

public record UsuarioDto(
    string Id,
    string NombreCompleto,
    string Email,
    IEnumerable<string> Roles,
    bool Activo,
    bool DobleFactorActivo,
    DateTimeOffset? UltimoAcceso,
    DateTimeOffset CreadoEn
);

// ══════════════════ INVITACIONES ══════════════════

public class CrearInvitacionDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>1 Administrador · 2 Operador</summary>
    [Range(1, 2)]
    public short Rol { get; set; } = 2;
}

/// <summary>
/// Resultado de crear una invitacion.
///
/// EnlaceTemporal solo viene con valor si el correo NO salio de
/// verdad. Cuando Resend este activo llega null y el panel deja de
/// mostrarlo automaticamente.
/// </summary>
public record InvitacionCreadaDto(
    int Id,
    string Email,
    bool CorreoEnviado,
    string? EnlaceTemporal
);

public record InvitacionDto(
    int Id,
    string Email,
    short Rol,
    string RolTexto,
    string InvitadoPorId,
    DateTimeOffset ExpiraEn,
    bool Usada,
    bool Revocada,
    bool Vigente,
    DateTimeOffset CreadoEn
);

public class AceptarInvitacionDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MaxLength(120), MinLength(3)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
