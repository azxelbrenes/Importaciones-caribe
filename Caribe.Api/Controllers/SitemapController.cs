using System.Text;
using System.Xml;
using Caribe.LogicaNegocio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caribe.Api.Controllers;

/// <summary>
/// sitemap.xml y robots.txt.
///
/// Se generan desde el backend y no como archivos fijos porque el
/// catalogo cambia: un archivo escrito a mano quedaria viejo el dia
/// que se publique un vehiculo, y Google seguiria mostrando fichas
/// que ya no existen.
///
/// Viven fuera de /api: los buscadores los piden en la raiz del
/// dominio. Caddy enruta esas dos direcciones hasta aqui.
/// </summary>
[ApiController]
[AllowAnonymous]
public class SitemapController : ControllerBase
{
    private readonly IVehiculoLN _ln;

    private const string Sitio = "https://importacionescaribecr.com";

    public SitemapController(IVehiculoLN ln) => _ln = ln;

    [HttpGet("/sitemap.xml")]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var r = await _ln.SlugsPublicadosAsync(ct);
        var vehiculos = r.Exitoso ? r.Valor! : [];

        var sb = new StringBuilder();

        await using var xml = XmlWriter.Create(new StringWriter(sb), new XmlWriterSettings
        {
            Indent = true,
            Async = true,
            Encoding = Encoding.UTF8
        });

        await xml.WriteStartDocumentAsync();
        await xml.WriteStartElementAsync(null, "urlset",
            "http://www.sitemaps.org/schemas/sitemap/0.9");

        // Las fijas. La prioridad es relativa dentro del propio sitio:
        // le dice a Google que el catalogo importa mas que los terminos.
        await Escribir(xml, "/", "daily", "1.0", null);
        await Escribir(xml, "/vehiculos", "daily", "0.9", null);
        await Escribir(xml, "/privacidad", "yearly", "0.2", null);
        await Escribir(xml, "/terminos", "yearly", "0.2", null);

        // Cada ficha publicada, con su fecha de modificacion: Google
        // vuelve a leer las que cambiaron en vez de todas.
        foreach (var (slug, actualizado) in vehiculos)
            await Escribir(xml, $"/vehiculos/{slug}", "weekly", "0.8", actualizado);

        await xml.WriteEndElementAsync();
        await xml.WriteEndDocumentAsync();
        await xml.FlushAsync();

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [EnableRateLimiting("general")]
    public IActionResult Robots()
    {
        // El panel y las paginas de un solo uso quedan fuera. No es
        // seguridad —quien tenga el enlace entra igual— sino evitar
        // que aparezcan en resultados de busqueda.
        var texto = $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /aceptar-invitacion
            Disallow: /restablecer
            Disallow: /api/

            Sitemap: {Sitio}/sitemap.xml
            """;

        return Content(texto, "text/plain", Encoding.UTF8);
    }

    private static async Task Escribir(
        XmlWriter xml, string ruta, string frecuencia, string prioridad,
        DateTimeOffset? actualizado)
    {
        await xml.WriteStartElementAsync(null, "url", null);

        await xml.WriteElementStringAsync(null, "loc", null, $"{Sitio}{ruta}");

        if (actualizado.HasValue)
            await xml.WriteElementStringAsync(null, "lastmod", null,
                actualizado.Value.ToString("yyyy-MM-dd"));

        await xml.WriteElementStringAsync(null, "changefreq", null, frecuencia);
        await xml.WriteElementStringAsync(null, "priority", null, prioridad);

        await xml.WriteEndElementAsync();
    }
}
