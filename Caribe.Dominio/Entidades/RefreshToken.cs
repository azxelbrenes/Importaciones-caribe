namespace Caribe.Dominio.Entidades;

/// <summary>
/// Token de larga duracion que renueva el acceso sin volver a pedir
/// la contrasena.
///
/// Se guarda solo el hash: si alguien obtiene una copia de la base,
/// no puede usar las sesiones activas.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }

    /// <summary>
    /// Hash del token que lo reemplazo. Si llega uno ya reemplazado,
    /// significa que alguien mas tiene una copia: se revocan todas
    /// las sesiones de ese usuario.
    /// </summary>
    public string? ReemplazadoPor { get; set; }

    public string? Ip { get; set; }

    public bool EstaActivo(DateTimeOffset ahora) =>
        RevocadoEn is null && ExpiraEn > ahora;
}
