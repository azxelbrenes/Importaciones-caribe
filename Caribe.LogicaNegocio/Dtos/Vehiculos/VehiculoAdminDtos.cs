using System.ComponentModel.DataAnnotations;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Dtos.Vehiculos;

/// <summary>
/// Vehiculo en el panel. Aqui SI van los costos y el margen: es la
/// vista interna del negocio y el endpoint exige rol de Gestion.
/// </summary>
public record VehiculoAdminDto(
    int Id,
    string Slug,
    string Marca,
    string Modelo,
    short Anio,
    int Kilometraje,
    string? Color,
    decimal CostoTotal,
    decimal Honorario,
    decimal PrecioPublicado,
    decimal Margen,
    short Estado,
    bool Destacado,
    bool AceptaFinanciamiento,
    int Visitas,
    int CantidadFotos,
    DateTimeOffset CreadoEn,
    DateTimeOffset? PublicadoEn
);

/// <summary>
/// Para llenar el formulario de edicion. Trae los cinco costos por
/// separado y los ids de marca y modelo, que el DTO de listado no
/// necesita pero el formulario si.
/// </summary>
public record VehiculoDetalleAdminDto(
    int Id,
    string Slug,
    int MarcaId,
    int ModeloId,
    short Anio,
    int Kilometraje,
    short Transmision,
    short Combustible,
    short Traccion,
    string? Color,
    string? Descripcion,
    decimal CostoVehiculo,
    decimal CostoFlete,
    decimal CostoImpuestos,
    decimal CostoTramites,
    decimal Honorario,
    decimal PrecioPublicado,
    short VigenciaDias,
    short? SemanasImportacionMin,
    short? SemanasImportacionMax,
    short Estado,
    bool Destacado,
    bool AceptaFinanciamiento
);

public class CrearVehiculoDto
{
    [Required] public int MarcaId { get; set; }
    [Required] public int ModeloId { get; set; }

    public short Anio { get; set; }
    public int Kilometraje { get; set; }

    public short Transmision { get; set; }
    public short Combustible { get; set; }
    public short Traccion { get; set; }

    [MaxLength(40)] public string? Color { get; set; }
    [MaxLength(4000)] public string? Descripcion { get; set; }

    public decimal CostoVehiculo { get; set; }
    public decimal CostoFlete { get; set; }
    public decimal CostoImpuestos { get; set; }
    public decimal CostoTramites { get; set; }
    public decimal Honorario { get; set; }

    public short VigenciaDias { get; set; } = 7;

    /// <summary>Opcionales: vacios, la ficha no muestra el tiempo.</summary>
    public short? SemanasImportacionMin { get; set; }
    public short? SemanasImportacionMax { get; set; }

    public bool Destacado { get; set; }
    public bool AceptaFinanciamiento { get; set; }

    /// <summary>
    /// El precio NO viaja en el DTO a proposito: lo calcula el
    /// servidor desde los costos. Si viniera del cliente, cualquiera
    /// podria publicar un vehiculo a un dolar.
    /// </summary>
    public bool PublicarAhora { get; set; }
}

public class ActualizarVehiculoDto : CrearVehiculoDto
{
    [Required] public int Id { get; set; }
}

public class CambiarEstadoDto
{
    [Required] public int Id { get; set; }
    public short NuevoEstado { get; set; }
}

public class ReordenarFotosDto
{
    [Required] public int VehiculoId { get; set; }

    [Required, MinLength(1)]
    public List<int> IdsEnOrden { get; set; } = [];
}

public record FotoSubidaDto(
    int Id,
    string Url,
    string UrlThumb,
    short Orden,
    bool EsPortada
);

/// <summary>Filtros del catalogo publico y del panel.</summary>
public class FiltroVehiculoDto : FiltroPaginado
{
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }
    public short? AnioDesde { get; set; }
    public short? AnioHasta { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public int? KilometrajeMax { get; set; }
    public short? Transmision { get; set; }
    public short? Combustible { get; set; }
    public short? Traccion { get; set; }
    public bool? AceptaFinanciamiento { get; set; }

    /// <summary>Solo en el panel: filtra por estado.</summary>
    public short? Estado { get; set; }

    public string? Busqueda { get; set; }

    /// <summary>reciente · precio_asc · precio_desc · km_asc · anio_desc</summary>
    public string OrdenarPor { get; set; } = "reciente";
}