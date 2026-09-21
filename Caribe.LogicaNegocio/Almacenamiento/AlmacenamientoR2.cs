using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Caribe.LogicaNegocio.Almacenamiento;

/// <summary>
/// Cloudflare R2. Usa el protocolo de S3, por eso sirve el SDK de AWS.
///
/// Frente al disco local tiene dos ventajas que importan: las fotos
/// sobreviven a una reinstalacion del servidor, y se sirven desde la
/// red de Cloudflare en vez de consumir ancho de banda del VPS.
/// </summary>
public class AlmacenamientoR2 : IAlmacenamiento
{
    private readonly IAmazonS3 _s3;
    private readonly R2Opciones _op;

    public AlmacenamientoR2(IAmazonS3 s3, IOptions<R2Opciones> op)
        => (_s3, _op) = (s3, op.Value);

    public async Task<string> GuardarAsync(
        Stream contenido, string rutaRelativa, string contentType,
        CancellationToken ct = default)
    {
        contenido.Position = 0;

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _op.Bucket,
            Key = rutaRelativa,
            InputStream = contenido,
            ContentType = contentType,

            // R2 no acepta la subida firmada por fragmentos que el SDK
            // de AWS usa por defecto desde 2025. Sin esto, toda subida
            // falla con "STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER not
            // implemented". La conexion sigue siendo HTTPS: se firma
            // la peticion completa, solo no cada fragmento.
            DisablePayloadSigning = true,

            // Un ano de cache: cada foto tiene nombre unico, asi que
            // una URL nunca va a apuntar a otro contenido. Sin esto,
            // el navegador volveria a descargar las mismas imagenes
            // en cada visita.
            Headers = { CacheControl = "public, max-age=31536000, immutable" }
        }, ct);

        return $"{_op.UrlPublica.TrimEnd('/')}/{rutaRelativa}";
    }

    public Task EliminarAsync(string rutaRelativa, CancellationToken ct = default)
        => _s3.DeleteObjectAsync(_op.Bucket, rutaRelativa, ct);

    public string RutaDesdeUrl(string url)
    {
        var baseUrl = _op.UrlPublica.TrimEnd('/');
        return url.StartsWith(baseUrl) ? url[(baseUrl.Length + 1)..] : url;
    }
}