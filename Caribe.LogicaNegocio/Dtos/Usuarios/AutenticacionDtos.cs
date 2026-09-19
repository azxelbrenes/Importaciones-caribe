using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Usuarios;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Codigo de la aplicacion de autenticacion. Llega solo en el
    /// segundo intento, despues de que el servidor pidio el codigo.
    /// </summary>
    public string? CodigoDobleFactor { get; set; }
}

public record TokenDto(
    string AccessToken,
    DateTimeOffset ExpiraEn,
    string NombreCompleto,
    string Email,
    IEnumerable<string> Roles
);

/// <summary>
/// Resultado del login.
///
/// Si la cuenta tiene doble factor y no llego el codigo, devuelve
/// RequiereDobleFactor con Token en null. Nunca se entrega un token
/// a medias: o esta completo o no esta.
/// </summary>
public record LoginResultadoDto(
    bool RequiereDobleFactor,
    TokenDto? Token
);

public class CambiarPasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string PasswordNueva { get; set; } = string.Empty;
}

// ══════════════════ RECUPERACION ══════════════════

public class SolicitarRecuperacionDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class RestablecerConTokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string PasswordNueva { get; set; } = string.Empty;

    [Required]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de restablecer la contrasena de otro usuario desde el
/// panel. Devuelve la temporal para poder pasarla a la persona.
/// </summary>
public record PasswordRestablecidaDto(
    string Email,
    string PasswordTemporal
);

// ══════════════════ DOBLE FACTOR ══════════════════

public record ConfigurarDobleFactorDto(
    string ClaveManual,
    string UriAutenticador
);

public class ActivarDobleFactorDto
{
    [Required]
    public string Codigo { get; set; } = string.Empty;
}

public class DesactivarDobleFactorDto
{
    /// <summary>
    /// Se exige la contrasena: si alguien encuentra una sesion
    /// abierta, no deberia poder quitar esta proteccion.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

// ══════════════════ SESIONES ══════════════════

public record SesionDto(
    int Id,
    string? Ip,
    DateTimeOffset CreadoEn,
    DateTimeOffset ExpiraEn,
    bool EsLaActual
);
