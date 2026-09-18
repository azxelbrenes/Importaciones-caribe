namespace Caribe.Dominio.Entidades;

/// <summary>
/// Token para restablecer la contrasena desde el enlace del correo.
///
/// No se usa el token de Identity directamente porque ese no permite
/// controlar vigencia ni uso unico con esta precision: un token de
/// recuperacion que sirva dos veces es una puerta abierta.
/// </summary>
public class TokenRecuperacion
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Corto a proposito: 30 minutos. Un enlace de recuperacion que
    /// vive horas en un buzon es una ventana innecesaria.
    /// </summary>
    public DateTimeOffset ExpiraEn { get; set; }

    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? UsadoEn { get; set; }

    /// <summary>Desde donde se pidio. Para detectar abuso.</summary>
    public string? IpSolicitud { get; set; }

    public bool EsValido(DateTimeOffset ahora) =>
        UsadoEn is null && ExpiraEn > ahora;
}
