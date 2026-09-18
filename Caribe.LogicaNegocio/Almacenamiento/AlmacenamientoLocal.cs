using Microsoft.Extensions.Options;

namespace Caribe.LogicaNegocio.Almacenamiento;

/// <summary>
/// Guarda en disco. Para desarrollo local, cuando no se quiere
/// depender de la red ni consumir el cupo de R2 probando.
/// </summary>
public class AlmacenamientoLocal : IAlmacenamiento
{
    private readonly AlmacenamientoOpciones _op;

    public AlmacenamientoLocal(IOptions<AlmacenamientoOpciones> op) => _op = op.Value;

    public async Task<string> GuardarAsync(
        Stream contenido, string rutaRelativa, string contentType,
        CancellationToken ct = default)
    {
        var limpia = Validar(rutaRelativa);

        var destino = Path.Combine(
            _op.RutaBase, limpia.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        await using var archivo = File.Create(destino);
        contenido.Position = 0;
        await contenido.CopyToAsync(archivo, ct);

        return $"{_op.UrlBase.TrimEnd('/')}/{limpia}";
    }

    public Task EliminarAsync(string rutaRelativa, CancellationToken ct = default)
    {
        var limpia = Validar(rutaRelativa);

        var destino = Path.Combine(
            _op.RutaBase, limpia.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(destino)) File.Delete(destino);

        return Task.CompletedTask;
    }

    public string RutaDesdeUrl(string url)
    {
        var baseUrl = _op.UrlBase.TrimEnd('/');
        return url.StartsWith(baseUrl) ? url[(baseUrl.Length + 1)..] : url;
    }

    /// <summary>
    /// Bloquea rutas que intenten salir de la carpeta base con "..".
    /// Sin esto se podria escribir en cualquier parte del disco del
    /// servidor pasando una ruta manipulada.
    /// </summary>
    private static string Validar(string ruta)
    {
        var limpia = ruta.Replace('\\', '/').TrimStart('/');

        if (limpia.Contains("..") || Path.IsPathRooted(limpia))
            throw new InvalidOperationException("Ruta de archivo no permitida.");

        return limpia;
    }
}
