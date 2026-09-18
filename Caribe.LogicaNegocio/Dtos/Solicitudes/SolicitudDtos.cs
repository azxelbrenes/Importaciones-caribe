using System.ComponentModel.DataAnnotations;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Dtos.Solicitudes;

public class CrearSolicitudDto
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Whatsapp { get; set; } = string.Empty;

    [MaxLength(60)] public string? MarcaTexto { get; set; }
    [MaxLength(80)] public string? ModeloTexto { get; set; }

    public short? AnioDesde { get; set; }
    public decimal? PresupuestoMin { get; set; }
    public decimal? PresupuestoMax { get; set; }

    public short? Transmision { get; set; }
    public short? Combustible { get; set; }

    [MaxLength(1000)] public string? Detalles { get; set; }

    /// <summary>0 Inicio · 1 Formulario · 2 Ficha · 3 No encontrada</summary>
    public short Origen { get; set; }

    public int? VehiculoId { get; set; }

    // ── Financiamiento ──
    public short FormaPago { get; set; }
    public short? PlazoMesesInteres { get; set; }

    /// <summary>
    /// Exigido por la Ley 8968. El servicio rechaza la solicitud sin
    /// esto: no es una casilla decorativa, es la prueba de que la
    /// persona autorizo el uso de sus datos.
    /// </summary>
    public bool Consentimiento { get; set; }
}

public record SolicitudDto(
    int Id,
    string Nombre,
    string Whatsapp,
    string? MarcaTexto,
    string? ModeloTexto,
    short? AnioDesde,
    decimal? PresupuestoMin,
    decimal? PresupuestoMax,
    short FormaPago,
    string FormaPagoTexto,
    short? PlazoMesesInteres,
    short Origen,
    string OrigenTexto,
    short Estado,
    string? AsignadaA,
    string? VehiculoSlug,
    DateTimeOffset CreadoEn,
    DateTimeOffset? AtendidaEn,
    int CantidadNotas
);

public record NotaDto(
    int Id,
    string UsuarioId,
    string Nota,
    DateTimeOffset CreadoEn
);

public record SolicitudDetalleDto(
    int Id,
    string Nombre,
    string Whatsapp,
    string? MarcaTexto,
    string? ModeloTexto,
    short? AnioDesde,
    decimal? PresupuestoMin,
    decimal? PresupuestoMax,
    short? Transmision,
    short? Combustible,
    string? Detalles,
    short FormaPago,
    string FormaPagoTexto,
    short? PlazoMesesInteres,
    short Origen,
    string OrigenTexto,
    short Estado,
    string? AsignadaA,
    string? VehiculoSlug,
    DateTimeOffset CreadoEn,
    DateTimeOffset? AtendidaEn,
    IReadOnlyList<NotaDto> Notas
);

public class ActualizarSolicitudDto
{
    [Required] public int Id { get; set; }
    public short Estado { get; set; }
    public string? AsignadaAId { get; set; }
}

public class CrearNotaDto
{
    [Required] public int SolicitudId { get; set; }

    [Required, MaxLength(2000)]
    public string Nota { get; set; } = string.Empty;
}

public class FiltroSolicitudDto : FiltroPaginado
{
    public short? Estado { get; set; }
    public string? AsignadaAId { get; set; }
    public string? Busqueda { get; set; }

    /// <summary>
    /// Por defecto la bandeja oculta las archivadas: con los meses
    /// son la mayoria y no se trabajan a diario.
    /// </summary>
    public bool IncluirArchivadas { get; set; }
}
