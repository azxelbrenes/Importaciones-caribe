using Caribe.Dominio.Enums;

namespace Caribe.Dominio.Entidades;

public class Vehiculo
{
    public int Id { get; set; }

    /// <summary>
    /// Identificador para la URL publica: "toyota-tacoma-trd-2023".
    /// Se genera del nombre y es unico. Mejor que el id numerico para
    /// buscadores y para quien comparte el enlace por WhatsApp.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public int MarcaId { get; set; }
    public int ModeloId { get; set; }

    public short Anio { get; set; }
    public int Kilometraje { get; set; }

    public Transmision Transmision { get; set; }
    public Combustible Combustible { get; set; }
    public Traccion Traccion { get; set; }

    public string? Color { get; set; }
    public string? Descripcion { get; set; }

    // ══════════════════ COSTOS ══════════════════
    // Estos cinco campos NUNCA salen al sitio publico. El DTO publico
    // no los incluye: son tipos distintos, no el mismo con campos
    // ocultos, para que un descuido en una proyeccion no los filtre.
    //
    // La competencia no debe poder deducir el margen del negocio.

    public decimal CostoVehiculo { get; set; }
    public decimal CostoFlete { get; set; }
    public decimal CostoImpuestos { get; set; }
    public decimal CostoTramites { get; set; }

    /// <summary>Ganancia del negocio. Se suma al costo para el precio.</summary>
    public decimal Honorario { get; set; }

    /// <summary>
    /// Precio al publico. Lo calcula el servidor al guardar, nunca
    /// llega desde el cliente: si viniera del navegador, cualquiera
    /// podria manipularlo.
    /// </summary>
    public decimal PrecioPublicado { get; set; }

    /// <summary>
    /// Dias que vale la cotizacion. Se muestra en la ficha porque bajo
    /// la Ley 7472 el precio publicado es una oferta vinculante, y los
    /// avaluos de Hacienda cambian.
    /// </summary>
    public short VigenciaDias { get; set; } = 7;

    // ══════════════════ FINANCIAMIENTO ══════════════════

    /// <summary>
    /// Si este vehiculo se ofrece con financiamiento.
    ///
    /// Es por vehiculo y no global porque no todos conviene
    /// financiarlos: uno de bajo precio o cerca de venderse al
    /// contado puede quedar fuera.
    ///
    /// Aunque este en true, las cuotas solo se muestran si la
    /// configuracion global esta operativa.
    /// </summary>
    public bool AceptaFinanciamiento { get; set; }

    // ══════════════════ ESTADO ══════════════════

    public EstadoVehiculo Estado { get; set; } = EstadoVehiculo.Borrador;

    /// <summary>Aparece en el inicio del sitio, junto al titular.</summary>
    public bool Destacado { get; set; }

    public int Visitas { get; set; }

    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }
    public DateTimeOffset? PublicadoEn { get; set; }
    public DateTimeOffset? VendidoEn { get; set; }

    public string? CreadoPorId { get; set; }

    // ══════════════════ NAVEGACION ══════════════════

    public Marca Marca { get; set; } = null!;
    public Modelo Modelo { get; set; } = null!;
    public ICollection<VehiculoFoto> Fotos { get; set; } = [];

    // ══════════════════ CALCULADAS ══════════════════
    // No son columnas: se marcan con Ignore en el contexto.

    public decimal CostoTotal =>
        CostoVehiculo + CostoFlete + CostoImpuestos + CostoTramites;

    public decimal Margen => PrecioPublicado - CostoTotal;

    /// <summary>
    /// Los unicos dos estados que el visitante ve. En trato sigue
    /// visible a proposito: un vehiculo con interes atrae mas interes.
    /// </summary>
    public bool EsVisibleAlPublico =>
        Estado is EstadoVehiculo.Disponible or EstadoVehiculo.EnTrato;
}
