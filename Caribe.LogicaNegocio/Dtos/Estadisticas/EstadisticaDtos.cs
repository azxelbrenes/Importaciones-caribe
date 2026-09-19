namespace Caribe.LogicaNegocio.Dtos.Estadisticas;

/// <summary>
/// Cifras del tablero.
///
/// Se calculan sobre el mes en curso: es el periodo con el que piensa
/// el negocio, no los ultimos 30 dias moviles.
/// </summary>
public record ResumenDto(
    int VehiculosPublicados,
    int VehiculosEnTrato,
    int VehiculosEnTransito,
    int VendidosMes,

    int SolicitudesNuevas,
    int SolicitudesMes,

    decimal IngresosMes,
    decimal MargenMes,

    int VisitasMes,

    /// <summary>
    /// Dias entre publicacion y venta. Dice cuanto tarda en rotar el
    /// inventario, que es lo que determina cuanto capital queda
    /// inmovilizado.
    /// </summary>
    double DiasPromedioVenta,

    /// <summary>
    /// Minutos entre que llega una solicitud y se atiende. Es la
    /// metrica que mas correlaciona con cerrar la venta.
    /// </summary>
    double MinutosPromedioRespuesta
);

public record VentaMesDto(
    short Anio,
    short Mes,
    int Cantidad,
    decimal Ingresos,
    decimal Margen
);

/// <summary>
/// Solicitudes por etapa. Muestra donde se atascan los clientes: si
/// hay treinta contactadas y ninguna cerrada, el problema esta en el
/// seguimiento, no en la captacion.
/// </summary>
public record EtapaPipelineDto(
    short Estado,
    string Etiqueta,
    int Cantidad
);

public record MarcaSolicitadaDto(
    string Marca,
    int Cantidad
);
