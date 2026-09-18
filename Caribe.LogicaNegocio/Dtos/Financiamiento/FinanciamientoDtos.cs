using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Financiamiento;

/// <summary>
/// Configuracion que ve y edita el panel.
/// </summary>
public record ConfiguracionFinanciamientoDto(
    bool Activo,
    decimal PorcentajePrima,
    decimal TasaAnual,
    short PlazoMinimoMeses,
    short PlazoMaximoMeses,
    string PlazosDisponibles,
    string? TextoLegal,
    bool EstaOperativo,
    DateTimeOffset ActualizadoEn
);

public class ActualizarFinanciamientoDto
{
    public bool Activo { get; set; }

    [Range(0, 100)]
    public decimal PorcentajePrima { get; set; } = 50m;

    /// <summary>
    /// El limite superior no es arbitrario: el Banco Central publica
    /// cada semestre un tope legal bajo la Ley 9859, y cobrar por
    /// encima configura usura. Este rango es un freno grueso; el
    /// administrador es responsable de respetar el tope vigente.
    /// </summary>
    [Range(0, 60)]
    public decimal TasaAnual { get; set; }

    [Range(1, 120)] public short PlazoMinimoMeses { get; set; } = 12;
    [Range(1, 120)] public short PlazoMaximoMeses { get; set; } = 36;

    [Required, MaxLength(60)]
    public string PlazosDisponibles { get; set; } = "12,24,36";

    [MaxLength(2000)]
    public string? TextoLegal { get; set; }
}

/// <summary>
/// Simulacion para el sitio publico. Devuelve todos los plazos
/// configurados con su cuota.
/// </summary>
public record SimulacionDto(
    decimal PrecioVehiculo,
    decimal Prima,
    IReadOnlyList<CuotaDto> Opciones,
    string? TextoLegal
);

public record CuotaDto(
    short PlazoMeses,
    decimal CuotaMensual,
    decimal TotalIntereses,
    decimal TotalAPagar,
    decimal TasaAnual
);
