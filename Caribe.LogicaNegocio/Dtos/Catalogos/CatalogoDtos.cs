using System.ComponentModel.DataAnnotations;

namespace Caribe.LogicaNegocio.Dtos.Catalogos;

public record MarcaDto(
    int Id,
    string Nombre,
    bool Activa,
    int CantidadModelos,

    /// <summary>
    /// Cuantos vehiculos la usan. Con esto el panel sabe si ofrecer
    /// eliminar o solo desactivar, sin una llamada aparte.
    /// </summary>
    int CantidadVehiculos
);

public record ModeloDto(
    int Id,
    int MarcaId,
    string Marca,
    string Nombre,
    bool Activo,
    int CantidadVehiculos
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
/// Resultado de la limpieza de duplicados. Se muestra al terminar
/// para que la persona sepa que se toco.
/// </summary>
public record LimpiezaDto(
    int MarcasFusionadas,
    int ModelosFusionados,
    int NombresCorregidos
);

/// <summary>
/// Opcion de un desplegable. Angular las consume de aqui para que
/// los textos no esten duplicados en dos lugares.
/// </summary>
public record OpcionDto(short Valor, string Texto);
