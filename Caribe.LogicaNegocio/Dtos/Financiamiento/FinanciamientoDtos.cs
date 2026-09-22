using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Financiamiento;

/// <summary>Configuracion que ve y edita el panel.</summary>
public record ConfiguracionFinanciamientoDto(
    bool Activo,
    decimal PorcentajePrima,
    short PlazoMinimoMeses,
    short PlazoMaximoMeses,
    string PlazosDisponibles,
    DateTimeOffset ActualizadoEn
);

public class ActualizarFinanciamientoDto
{
    public bool Activo { get; set; }

    [Range(0, 100)]
    public decimal PorcentajePrima { get; set; } = 50m;

    [Range(1, 120)] public short PlazoMinimoMeses { get; set; } = 12;
    [Range(1, 120)] public short PlazoMaximoMeses { get; set; } = 36;

    [Required, MaxLength(60)]
    public string PlazosDisponibles { get; set; } = "12,24,36";
}

/// <summary>
/// Lo que ve el sitio publico: si se ofrece, la prima y los plazos.
/// Nada de tasas: el interes lo da el dueno por WhatsApp.
/// </summary>
public record FinanciamientoPublicoDto(
    bool Activo,
    decimal PorcentajePrima,
    IReadOnlyList<short> Plazos
);
