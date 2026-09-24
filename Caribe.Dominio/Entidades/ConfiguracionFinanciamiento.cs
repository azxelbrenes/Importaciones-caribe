namespace Caribe.Dominio.Entidades;

/// <summary>
/// Parametros del financiamiento. Una sola fila en la base.
///
/// El sitio muestra la cuota mensual estimada de cada plazo, pero NO
/// el porcentaje de interes: la cuota se calcula en el servidor y al
/// navegador solo llega el resultado.
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

    /// <summary>
    /// Interes que se suma UNA sola vez sobre el monto financiado, sin
    /// importar el plazo. 15 = el cliente paga lo financiado mas 15%,
    /// repartido en las cuotas. Nunca se envia al sitio publico.
    /// </summary>
    public decimal PorcentajeInteres { get; set; } = 15m;

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

    /// <summary>Lo que el cliente paga de entrada.</summary>
    public decimal Prima(decimal precio) =>
        Math.Round(precio * PorcentajePrima / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Cuota mensual: (precio - prima) * (1 + interes%) / meses.
    ///
    /// Interes simple, una sola vez sobre lo financiado: el negocio
    /// cobra el mismo porcentaje a 12 que a 36 meses. Se redondea al
    /// centavo hacia arriba para que la suma de cuotas nunca quede por
    /// debajo del total.
    /// </summary>
    public decimal CuotaMensual(decimal precio, short meses)
    {
        if (meses <= 0) return 0m;

        var financiado = precio - Prima(precio);
        var total = financiado * (1m + PorcentajeInteres / 100m);

        return Math.Ceiling(total / meses * 100m) / 100m;
    }

    public IEnumerable<short> Plazos() =>
        PlazosDisponibles
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => short.TryParse(p.Trim(), out var v) ? v : (short)0)
            .Where(p => p >= PlazoMinimoMeses && p <= PlazoMaximoMeses)
            .Distinct()
            .OrderBy(p => p);
}
