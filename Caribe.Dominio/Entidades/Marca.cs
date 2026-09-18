namespace Caribe.Dominio.Entidades;

public class Marca
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Las marcas no se borran: se desactivan. Borrar una que tiene
    /// vehiculos asociados dejaria registros huerfanos y rompería el
    /// historial de ventas.
    /// </summary>
    public bool Activa { get; set; } = true;

    public ICollection<Modelo> Modelos { get; set; } = [];
}
