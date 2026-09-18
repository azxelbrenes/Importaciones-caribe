namespace Caribe.Dominio.Entidades;

/// <summary>
/// Resultado del calculo para un vehiculo y un plazo.
///
/// NO es una entidad de base de datos: se calcula al vuelo cada vez.
/// Guardarlo seria un error, porque la tasa puede cambiar y las
/// cuotas viejas quedarian desactualizadas sin aviso.
/// </summary>
public record PlanFinanciamiento(
    short PlazoMeses,
    decimal Prima,
    decimal MontoFinanciado,
    decimal CuotaMensual,
    decimal TotalIntereses,
    decimal TotalAPagar,
    decimal TasaAnual
);

/// <summary>
/// Calculo de cuotas con el sistema frances: cuota fija, donde la
/// parte de interes baja y la de capital sube cada mes.
///
/// Es el mismo que usan los bancos, asi que el cliente puede comparar
/// la oferta con la de su banco sin sorpresas.
/// </summary>
public static class CalculadoraFinanciamiento
{
    /// <summary>
    /// Calcula el plan para un precio y un plazo.
    ///
    /// Devuelve null si la configuracion no esta operativa: sin tasa
    /// puesta o sin activar, no se ofrece financiamiento.
    /// </summary>
    public static PlanFinanciamiento? Calcular(
        decimal precio,
        short plazoMeses,
        ConfiguracionFinanciamiento config)
    {
        if (!config.EstaOperativo) return null;
        if (precio <= 0 || plazoMeses <= 0) return null;

        if (plazoMeses < config.PlazoMinimoMeses ||
            plazoMeses > config.PlazoMaximoMeses) return null;

        var prima = Redondear(precio * (config.PorcentajePrima / 100m));
        var financiado = precio - prima;

        // Tasa mensual: la anual dividida entre doce y pasada a decimal.
        var tasaMensual = config.TasaAnual / 100m / 12m;

        decimal cuota;

        if (tasaMensual == 0)
        {
            cuota = financiado / plazoMeses;
        }
        else
        {
            // Sistema frances:  C = P · i / (1 - (1+i)^-n)
            //
            // Se usa double para la potencia porque decimal no tiene
            // Math.Pow, y se vuelve a decimal para el resultado: los
            // montos de dinero nunca se guardan en punto flotante.
            var i = (double)tasaMensual;
            var n = plazoMeses;
            var factor = i / (1 - Math.Pow(1 + i, -n));

            cuota = Redondear(financiado * (decimal)factor);
        }

        var totalCuotas = Redondear(cuota * plazoMeses);
        var intereses = totalCuotas - financiado;

        return new PlanFinanciamiento(
            PlazoMeses: plazoMeses,
            Prima: prima,
            MontoFinanciado: financiado,
            CuotaMensual: cuota,
            TotalIntereses: intereses,
            // El total incluye la prima: es lo que el cliente termina
            // pagando por el vehiculo. La Ley 7472 exige mostrarlo.
            TotalAPagar: prima + totalCuotas,
            TasaAnual: config.TasaAnual
        );
    }

    /// <summary>Todos los plazos configurados, para la ficha del vehiculo.</summary>
    public static IEnumerable<PlanFinanciamiento> CalcularTodos(
        decimal precio, ConfiguracionFinanciamiento config)
    {
        if (!config.EstaOperativo) return [];

        return config.Plazos()
            .Select(p => Calcular(precio, p, config))
            .Where(p => p is not null)
            .Select(p => p!);
    }

    /// <summary>Dos decimales, redondeo comercial.</summary>
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
