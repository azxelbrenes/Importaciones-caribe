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
/// Trae la prima en dinero, los plazos y la cuota mensual de cada uno,
/// ya calculada. El porcentaje de interes NO viaja: el navegador solo
/// recibe el resultado.
/// </summary>
public record FinanciamientoVehiculoDto(
    decimal PorcentajePrima,
    decimal Prima,
    IReadOnlyList<short> Plazos,
    IReadOnlyList<CuotaFinanciamientoDto> Cuotas
);

/// <summary>Cuota mensual estimada para un plazo.</summary>
public record CuotaFinanciamientoDto(short Meses, decimal Cuota);

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

    /// <summary>
    /// Dias que vale la cotizacion. Es lo unico que queda del calculo:
    /// los montos por linea —cuanto costo en EE. UU., cuanto se pago de
    /// impuestos— no salen del servidor. Ocultarlos en la pagina no
    /// alcanzaria: seguirian viajando en la respuesta del API, y
    /// cualquiera podria leerlos desde el navegador.
    /// </summary>
    short VigenciaDias,

    /// <summary>
    /// Rango estimado de la importacion, en semanas. Null si el
    /// negocio no lo indico para este vehiculo.
    /// </summary>
    short? SemanasImportacionMin,
    short? SemanasImportacionMax,
    IReadOnlyList<FotoDto> Fotos,

    /// <summary>Null si el vehiculo no se financia o esta apagado.</summary>
    FinanciamientoVehiculoDto? Financiamiento
);