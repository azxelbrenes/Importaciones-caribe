namespace Caribe.Dominio.Entidades;

/// <summary>
/// Parametros del financiamiento. Una sola fila en la base.
///
/// La tasa NO esta fija en el codigo a proposito: en Costa Rica el
/// Banco Central publica cada semestre un tope legal a las tasas de
/// interes (Ley 9859), y cobrar por encima configura usura. Tener
/// que recompilar para ajustarla seria un riesgo innecesario.
///
/// PENDIENTE LEGAL: antes de publicar cuotas en el sitio, el abogado
/// debe confirmar que el negocio puede ofrecer financiamiento directo
/// y bajo que figura. La Ley 7472 obliga ademas a mostrar el costo
/// total del credito, no solo la cuota mensual.
/// </summary>
public class ConfiguracionFinanciamiento
{
    public int Id { get; set; }

    /// <summary>
    /// Mientras esto sea false, el sitio publico no muestra cuotas.
    /// Es el interruptor para no publicar nada hasta tener el visto
    /// bueno legal.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>
    /// Porcentaje que el cliente paga por adelantado. 50 = mitad.
    /// </summary>
    public decimal PorcentajePrima { get; set; } = 50m;

    /// <summary>
    /// Tasa nominal anual en porcentaje. 18.5 = 18,5% al ano.
    ///
    /// Arranca en cero: sin un valor puesto por el administrador,
    /// el calculo no se ofrece.
    /// </summary>
    public decimal TasaAnual { get; set; }

    public short PlazoMinimoMeses { get; set; } = 12;

    /// <summary>
    /// Tres anos es el limite que definio el negocio. Plazos mas
    /// largos aumentan el riesgo de impago sobre un bien que ya
    /// esta a nombre del comprador.
    /// </summary>
    public short PlazoMaximoMeses { get; set; } = 36;

    /// <summary>
    /// Plazos que se ofrecen, separados por coma: "12,24,36".
    /// Se guardan asi en vez de una tabla aparte porque son tres
    /// valores que casi nunca cambian.
    /// </summary>
    public string PlazosDisponibles { get; set; } = "12,24,36";

    /// <summary>
    /// Texto legal que acompana toda cuota mostrada. Lo redacta el
    /// abogado; se guarda aqui para poder corregirlo sin desplegar.
    /// </summary>
    public string? TextoLegal { get; set; }

    public DateTimeOffset ActualizadoEn { get; set; }
    public string? ActualizadoPorId { get; set; }

    /// <summary>
    /// El financiamiento solo se ofrece si esta activo Y hay una tasa
    /// puesta. Las dos condiciones, no una: activarlo con tasa cero
    /// mostraria cuotas sin interes, que no es lo acordado.
    /// </summary>
    public bool EstaOperativo => Activo && TasaAnual > 0;

    public IEnumerable<short> Plazos() =>
        PlazosDisponibles
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => short.TryParse(p.Trim(), out var v) ? v : (short)0)
            .Where(p => p >= PlazoMinimoMeses && p <= PlazoMaximoMeses)
            .OrderBy(p => p);
}
