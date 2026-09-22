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
    DateTimeOffset? UltimoAcceso,
    DateTimeOffset CreadoEn
);
