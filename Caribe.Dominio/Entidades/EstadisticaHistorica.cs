namespace Caribe.Dominio.Entidades;

/// <summary>
/// Conteo mensual de solicitudes por estado, guardado antes de purgar.
///
/// Existe por un problema concreto: si se borran las solicitudes
/// descartadas para limpiar la bandeja, la tasa de conversion sube
/// artificialmente. Con 100 solicitudes y 20 cerradas, la conversion
/// real es 20%; si se borran 50 descartadas, el panel mostraria 40%.
///
/// Ese numero es el que le dice al dueno si su sitio funciona. Verlo
/// inflado lo llevaria a conclusiones equivocadas sobre su publicidad.
///
/// El embudo suma los registros vivos mas estos historicos.
/// </summary>
public class EstadisticaHistorica
{
    public int Id { get; set; }

    public short Anio { get; set; }
    public short Mes { get; set; }

    public int Nuevas { get; set; }
    public int Contactadas { get; set; }
    public int EnProceso { get; set; }
    public int Cerradas { get; set; }
    public int Descartadas { get; set; }

    /// <summary>Cuantas de ese mes ya se purgaron de la tabla viva.</summary>
    public int Purgadas { get; set; }

    public DateTimeOffset ActualizadoEn { get; set; }
}
