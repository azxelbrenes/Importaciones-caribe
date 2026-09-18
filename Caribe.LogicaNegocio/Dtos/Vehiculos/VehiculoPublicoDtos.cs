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
/// Opcion de financiamiento para la ficha. Se calcula al vuelo y
/// solo aparece si la configuracion global esta operativa.
/// </summary>
public record OpcionFinanciamientoDto(
    short PlazoMeses,
    decimal Prima,
    decimal CuotaMensual,
    decimal TotalAPagar,
    decimal TasaAnual
);

public record VehiculoDetalleDto(
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
    IReadOnlyList<OpcionFinanciamientoDto> Financiamiento,
    string? TextoLegalFinanciamiento
);
