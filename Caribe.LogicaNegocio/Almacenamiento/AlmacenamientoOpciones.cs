namespace Caribe.LogicaNegocio.Almacenamiento;

public class AlmacenamientoOpciones
{
    public const string Seccion = "Almacenamiento";

    public string RutaBase { get; set; } = "wwwroot/uploads";
    public string UrlBase { get; set; } = "/uploads";

    public int MaxMegabytesPorArchivo { get; set; } = 10;
    public int MaxFotosPorVehiculo { get; set; } = 12;
}

public class R2Opciones
{
    public const string Seccion = "R2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Dominio propio del bucket: fotos.importacionescaribecr.com
    ///
    /// Se usa uno propio y no la URL generica de R2 para que las fotos
    /// se sirvan desde la marca, y para que cambiar de proveedor no
    /// obligue a reescribir las direcciones ya guardadas.
    /// </summary>
    public string UrlPublica { get; set; } = string.Empty;

    public string Endpoint => $"https://{AccountId}.r2.cloudflarestorage.com";

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(UrlPublica);
}
