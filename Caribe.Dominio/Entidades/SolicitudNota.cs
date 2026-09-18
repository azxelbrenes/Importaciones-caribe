namespace Caribe.Dominio.Entidades;

/// <summary>
/// Nota interna sobre una solicitud. El cliente nunca las ve.
///
/// Existe para que el seguimiento no dependa de la memoria de quien
/// atendio: si una persona renuncia, el historial queda.
/// </summary>
public class SolicitudNota
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string UsuarioId { get; set; } = string.Empty;

    public string Nota { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; }

    public Solicitud Solicitud { get; set; } = null!;
}
