namespace Caribe.Dominio.Entidades;

/// <summary>
/// Parametros del financiamiento. Una sola fila en la base.
///
/// El sitio NO publica tasas ni cuotas: el cliente elige el plazo y
/// el interes se lo da el dueno directamente por WhatsApp. Por eso
/// aqui no hay tasa ni texto legal — no se publican condiciones de
/// credito, solo que el vehiculo se puede financiar y en que plazos.
/// </summary>
public class ConfiguracionFinanciamiento
{
    public int Id { get; set; }

    /// <summary>
    /// Mientras sea false, el sitio no ofrece financiamiento en ningun
    /// vehiculo, aunque este marcado como financiable.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Porcentaje que el cliente paga por adelantado. 50 = mitad.</summary>
    public decimal PorcentajePrima { get; set; } = 50m;

    public short PlazoMinimoMeses { get; set; } = 12;

    /// <summary>
    /// Tres anos es el limite que definio el negocio. Plazos mas largos
    /// aumentan el riesgo sobre un bien que ya esta a nombre del comprador.
    /// </summary>
    public short PlazoMaximoMeses { get; set; } = 36;

    /// <summary>
    /// Plazos que el cliente puede elegir, separados por coma: "12,24,36".
    /// Tres valores que casi nunca cambian no justifican una tabla aparte.
    /// </summary>
    public string PlazosDisponibles { get; set; } = "12,24,36";

    public DateTimeOffset ActualizadoEn { get; set; }
    public string? ActualizadoPorId { get; set; }

    public IEnumerable<short> Plazos() =>
        PlazosDisponibles
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => short.TryParse(p.Trim(), out var v) ? v : (short)0)
            .Where(p => p >= PlazoMinimoMeses && p <= PlazoMaximoMeses)
            .Distinct()
            .OrderBy(p => p);
}
