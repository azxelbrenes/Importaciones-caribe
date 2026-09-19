namespace Caribe.LogicaNegocio.Correo;

public class CorreoOpciones
{
    public const string Seccion = "Correo";

    public string ApiKey { get; set; } = string.Empty;
    public string Remitente { get; set; } = "no-responder@importacionescaribecr.com";
    public string NombreRemitente { get; set; } = "Importaciones del Caribe CR";

    /// <summary>Base para armar los enlaces que van en los correos.</summary>
    public string UrlSitio { get; set; } = "https://importacionescaribecr.com";

    /// <summary>
    /// A donde avisar cuando llega una solicitud nueva. Vacio
    /// desactiva el aviso.
    /// </summary>
    public string? CorreoAvisos { get; set; }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ApiKey);
}
