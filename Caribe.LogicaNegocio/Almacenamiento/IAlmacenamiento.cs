namespace Caribe.LogicaNegocio.Almacenamiento;

/// <summary>
/// Abstrae DONDE viven los archivos. La logica de negocio no sabe si
/// es disco local o Cloudflare R2: solo pide guardar y recibe una URL.
///
/// Cambiar de proveedor es cambiar el registro en Program.cs.
/// </summary>
public interface IAlmacenamiento
{
    Task<string> GuardarAsync(
        Stream contenido, string rutaRelativa, string contentType,
        CancellationToken ct = default);

    Task EliminarAsync(string rutaRelativa, CancellationToken ct = default);

    /// <summary>
    /// Convierte una URL publica de vuelta a su ruta interna, para
    /// poder borrar el archivo. Cada implementacion sabe como es su
    /// propia URL.
    /// </summary>
    string RutaDesdeUrl(string url);
}
