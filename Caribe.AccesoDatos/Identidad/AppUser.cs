using Microsoft.AspNetCore.Identity;

namespace Caribe.AccesoDatos.Identidad;

/// <summary>
/// Vive en AccesoDatos y no en Dominio a proposito: asi la capa de
/// dominio no depende del paquete de Identity y se mantiene pura.
///
/// Las entidades del dominio guardan el id de usuario como texto,
/// sin propiedad de navegacion.
/// </summary>
public class AppUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Desactivar en vez de borrar. Un usuario eliminado dejaria
    /// huerfanas las notas y la auditoria que creo.
    /// </summary>
    public bool Activo { get; set; } = true;

    public string? InvitadoPorId { get; set; }
    public DateTimeOffset? UltimoAcceso { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}

public static class Roles
{
    public const string SuperAdministrador = "SuperAdministrador";
    public const string Administrador      = "Administrador";
    public const string Operador           = "Operador";

    public static readonly string[] Todos =
        [SuperAdministrador, Administrador, Operador];

    /// <summary>Vehiculos, catalogo, estadisticas y financiamiento.</summary>
    public const string Gestion = $"{SuperAdministrador},{Administrador}";

    /// <summary>Solicitudes. Incluye al Operador.</summary>
    public const string Atencion = $"{SuperAdministrador},{Administrador},{Operador}";
}
