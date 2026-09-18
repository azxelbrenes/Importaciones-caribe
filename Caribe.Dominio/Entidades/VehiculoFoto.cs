namespace Caribe.Dominio.Entidades;

public class VehiculoFoto
{
    public int Id { get; set; }
    public int VehiculoId { get; set; }

    /// <summary>Imagen grande para la ficha: 1200x900 en WebP.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Miniatura para las tarjetas del catalogo: 400x300.</summary>
    public string UrlThumb { get; set; } = string.Empty;

    public short Orden { get; set; }

    /// <summary>
    /// La que sale en el catalogo. Un indice unico parcial impide que
    /// haya dos portadas del mismo vehiculo.
    /// </summary>
    public bool EsPortada { get; set; }

    public DateTimeOffset CreadoEn { get; set; }

    public Vehiculo Vehiculo { get; set; } = null!;
}
