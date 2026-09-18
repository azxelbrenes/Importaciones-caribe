using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Catalogos;

public record MarcaDto(
    int Id,
    string Nombre,
    bool Activa,
    int CantidadModelos
);

public record ModeloDto(
    int Id,
    int MarcaId,
    string Marca,
    string Nombre,
    bool Activo
);

public class CrearMarcaDto
{
    [Required, MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;
}

public class CrearModeloDto
{
    [Required]
    public int MarcaId { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// Opcion de un desplegable. Angular las consume de aqui para que los
/// textos no esten duplicados en dos lugares: si el backend dice
/// "En tránsito" y el frontend "En transito", el cliente pregunta si
/// son estados distintos.
/// </summary>
public record OpcionDto(short Valor, string Texto);
