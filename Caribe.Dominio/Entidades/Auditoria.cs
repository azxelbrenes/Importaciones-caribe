using Caribe.Dominio.Enums;
using System.Text.Json;

namespace Caribe.Dominio.Entidades;

/// <summary>
/// Registro de toda operacion de escritura.
///
/// Sirve para dos cosas: investigar un incidente, y responder la
/// pregunta "quien cambio este precio y cuando". Los datos se guardan
/// como jsonb para poder consultarlos sin esquema fijo.
/// </summary>
public class Auditoria
{
    public long Id { get; set; }

    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public AccionAuditoria Accion { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public string? Ip { get; set; }

    public JsonDocument? DatosAntes { get; set; }
    public JsonDocument? DatosDespues { get; set; }

    public DateTimeOffset CreadoEn { get; set; }
}
