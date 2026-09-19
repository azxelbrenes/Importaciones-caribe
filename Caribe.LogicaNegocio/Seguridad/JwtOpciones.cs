namespace Caribe.LogicaNegocio.Seguridad;

public class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Corto a proposito: si roban el token, dura poco. El interceptor
    /// del frontend lo renueva solo, asi que el usuario no lo nota.
    /// </summary>
    public int MinutosAccessToken { get; set; } = 15;

    public int DiasRefreshToken { get; set; } = 7;

    /// <summary>Vigencia del enlace de recuperacion de contrasena.</summary>
    public int MinutosTokenRecuperacion { get; set; } = 30;
}
