using Caribe.Dominio.Enums;

namespace Caribe.Dominio.Entidades;

public class Solicitud
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;

    // ══════════════════ QUE BUSCA ══════════════════
    // Texto libre a proposito: en importacion bajo pedido, alguien
    // puede querer una marca que todavia no esta en el catalogo.
    // Encerrarlo en un desplegable dejaria fuera a esos clientes.

    public string? MarcaTexto { get; set; }
    public string? ModeloTexto { get; set; }
    public short? AnioDesde { get; set; }

    public decimal? PresupuestoMin { get; set; }
    public decimal? PresupuestoMax { get; set; }

    public Transmision? Transmision { get; set; }
    public Combustible? Combustible { get; set; }

    public string? Detalles { get; set; }

    // ══════════════════ SEGUIMIENTO ══════════════════
    // ── Financiamiento ──

    /// <summary>
    /// Cómo piensa pagar. Cambia por completo la conversación de
    /// venta: un cliente de contado se cierra en días, uno financiado
    /// necesita revisar plazos y firmar garantía.
    /// </summary>
    public FormaPago FormaPago { get; set; } = FormaPago.PorDefinir;

    /// <summary>
    /// Plazo que eligió en el simulador, si lo usó. Guardarlo evita
    /// preguntarle de nuevo lo que ya respondió en el sitio.
    /// </summary>
    public short? PlazoMesesInteres { get; set; }

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Nueva;
    public OrigenSolicitud Origen { get; set; }

    public string? AsignadaAId { get; set; }

    /// <summary>Si vino desde la ficha de un vehiculo concreto.</summary>
    public int? VehiculoId { get; set; }

    /// <summary>
    /// Exigido por la Ley 8968: sin consentimiento explicito no se
    /// pueden tratar los datos. Se guarda como prueba de que se dio.
    /// </summary>
    public bool Consentimiento { get; set; }

    /// <summary>
    /// IP de origen. Sirve para el anti duplicado y para investigar
    /// envios automatizados.
    /// </summary>
    public string? IpOrigen { get; set; }

    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? AtendidaEn { get; set; }

    /// <summary>
    /// Cuando se saco de la bandeja. Se conserva el registro porque
    /// alimenta las estadisticas de conversion.
    /// </summary>
    public DateTimeOffset? ArchivadaEn { get; set; }

    public Vehiculo? Vehiculo { get; set; }
    public ICollection<SolicitudNota> Notas { get; set; } = [];

}
   