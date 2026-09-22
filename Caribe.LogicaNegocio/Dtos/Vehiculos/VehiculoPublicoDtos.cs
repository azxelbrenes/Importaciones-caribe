namespace Caribe.LogicaNegocio.Dtos.Vehiculos;

/// <summary>
/// Vehiculo en el catalogo publico.
///
/// NO tiene costos, honorario ni margen. Es un tipo distinto del DTO
/// administrativo, no el mismo con campos ocultos: asi un descuido en
/// una proyeccion no puede filtrar lo que no existe en el tipo.
///
/// La competencia no debe poder deducir el margen del negocio.
/// </summary>
public record VehiculoDto(
    string Slug,
    string Marca,
    string Modelo,
    short Anio,
    int Kilometraje,
    string? Color,
    decimal PrecioPublicado,
    short Estado,
    bool Destacado,
    bool AceptaFinanciamiento,
    string? FotoPortada
);

/// <summary>
/// Desglose que ve el visitante.
///
/// El honorario NO aparece como linea propia: va sumado dentro de
/// "Tramites". Mostrarlo por separado le diria a la competencia
/// exactamente cuanto gana el negocio por vehiculo.
/// </summary>
public record DesglosePrecioDto(
    decimal Vehiculo,
    decimal Flete,
    decimal Impuestos,
    decimal Tramites,
    decimal Total,
    short VigenciaDias
);

public record FotoDto(
    string Url,
    string UrlThumb,
    short Orden,
    bool EsPortada
);

/// <summary>
/// Financiamiento de un vehiculo en la ficha. Solo aparece si el
/// vehiculo lo acepta y la configuracion esta activa.
///
/// Muestra la prima en dinero —dato util y que no revela ningun
/// interes— y los plazos entre los que el cliente puede elegir. Las
/// condiciones se las da el dueno por WhatsApp.
/// </summary>
public record FinanciamientoVehiculoDto(
    decimal PorcentajePrima,
    decimal Prima,
    IReadOnlyList<short> Plazos
);

public record VehiculoDetalleDto(
    /// <summary>
    /// Se expone para que el formulario de interes pueda enlazar la
    /// solicitud con el vehiculo. No revela nada: los costos no estan
    /// en este tipo, y el catalogo ya es publico.
    /// </summary>
    int Id,

    string Slug,
    string Marca,
    string Modelo,
    short Anio,
    int Kilometraje,
    string Transmision,
    string Combustible,
    string Traccion,
    string? Color,
    string? Descripcion,
    decimal PrecioPublicado,
    short Estado,
    DesglosePrecioDto Desglose,
    IReadOnlyList<FotoDto> Fotos,

    /// <summary>Null si el vehiculo no se financia o esta apagado.</summary>
    FinanciamientoVehiculoDto? Financiamiento
);
