namespace Caribe.Dominio.Entidades;

public class Modelo
{
    public int Id { get; set; }
    public int MarcaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Marca Marca { get; set; } = null!;
}
