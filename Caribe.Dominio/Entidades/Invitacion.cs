using Caribe.Dominio.Enums;

namespace Caribe.Dominio.Entidades;

public class Invitacion
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public RolInvitacion Rol { get; set; }

    /// <summary>
    /// Solo el hash. El valor en claro existe una vez, en el correo.
    /// Una copia de la base no permite usar invitaciones pendientes.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public string InvitadoPorId { get; set; } = string.Empty;

    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? UsadaEn { get; set; }
    public DateTimeOffset? RevocadaEn { get; set; }

    public bool EsValida(DateTimeOffset ahora) =>
        UsadaEn is null && RevocadaEn is null && ExpiraEn > ahora;
}
