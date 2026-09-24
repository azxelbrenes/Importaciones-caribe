namespace Caribe.LogicaNegocio.Dtos.Usuarios;

/// <summary>
/// Datos de la propia cuenta, para la pantalla Mi cuenta.
///
/// Existe porque el token no dice si el doble factor esta activo, y
/// el listado de usuarios es solo del propietario. Sin esto, un
/// operador no tendria forma de saber el estado de su propia cuenta.
/// </summary>
public record PerfilDto(
    string NombreCompleto,
    string Email,
    IEnumerable<string> Roles,
    bool DobleFactorActivo,

    /// <summary>
    /// Codigos de respaldo sin usar. Se muestra en Mi cuenta: quedarse
    /// sin ellos y perder el telefono es quedarse afuera.
    /// </summary>
    int CodigosRespaldoRestantes,
    DateTimeOffset? UltimoAcceso,
    DateTimeOffset CreadoEn
);
