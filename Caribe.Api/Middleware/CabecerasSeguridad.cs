namespace Caribe.Api.Middleware;

/// <summary>
/// Cabeceras de seguridad en todas las respuestas.
///
/// La mas importante es la CSP: las demas mitigan un ataque, esta
/// impide que ocurra. Le dice al navegador que orígenes puede
/// ejecutar, y bloquea todo lo demas aunque este inyectado en el HTML.
/// </summary>
public class CabecerasSeguridad
{
    private readonly RequestDelegate _siguiente;
    private readonly IWebHostEnvironment _entorno;

    public CabecerasSeguridad(RequestDelegate siguiente, IWebHostEnvironment entorno)
        => (_siguiente, _entorno) = (siguiente, entorno);

    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Impide que el navegador adivine el tipo de archivo. Sin esto,
        // un .txt con contenido HTML podria ejecutarse como pagina.
        h["X-Content-Type-Options"] = "nosniff";

        // Corta el clickjacking: superponer un sitio falso sobre el
        // real dentro de un iframe para robar clics.
        h["X-Frame-Options"] = "DENY";

        // Al salir del sitio solo se envia el dominio, no la ruta
        // completa. Evita filtrar direcciones internas del panel.
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Funciones del navegador que este sitio no usa. Si alguien
        // inyecta codigo, no puede pedir la camara ni la ubicacion.
        h["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

        if (ctx.Request.IsHttps)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        h["Content-Security-Policy"] = ConstruirCsp();

        // Kestrel anuncia el servidor por defecto: es informacion
        // gratis para quien busca vulnerabilidades de una version.
        h.Remove("Server");
        h.Remove("X-Powered-By");
        h.Remove("X-AspNet-Version");

        await _siguiente(ctx);
    }

    private string ConstruirCsp()
    {
        // 'unsafe-inline' en estilos es necesario porque Angular
        // inyecta estilos en linea al renderizar. En SCRIPTS no se
        // permite: ahi esta el valor real de la politica, porque un
        // <script> inyectado simplemente no se ejecuta.
        var reglas = new List<string>
        {
            "default-src 'self'",
            // El medidor de Cloudflare es el unico script externo que
            // se permite. Sin esta linea la CSP lo bloquea y no mide
            // nada, sin dar ningun aviso visible.
            "script-src 'self' https://static.cloudflareinsights.com",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
            "font-src 'self' https://fonts.gstatic.com data:",

            // Las fotos viven en R2, bajo el subdominio propio.
            "img-src 'self' data: blob: https:",

            // El medidor manda los datos a esta direccion.
            "connect-src 'self' https://cloudflareinsights.com",
            "form-action 'self'",
            "base-uri 'self'",
            "frame-ancestors 'none'",
            "object-src 'none'"
        };

        if (!_entorno.IsDevelopment())
            reglas.Add("upgrade-insecure-requests");

        return string.Join("; ", reglas);
    }
}
