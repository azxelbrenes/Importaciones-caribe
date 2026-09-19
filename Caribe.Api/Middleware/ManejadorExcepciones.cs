using System.Net;
using System.Text.Json;

namespace Caribe.Api.Middleware;

/// <summary>
/// Atrapa lo que ningun controlador manejo y devuelve JSON.
///
/// Sin esto, una excepcion no controlada devolveria HTML con la traza
/// de pila: el nombre de las clases, las rutas del servidor y a veces
/// fragmentos de la consulta SQL. Es informacion gratis para quien
/// este buscando por donde entrar.
/// </summary>
public class ManejadorExcepciones
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorExcepciones> _logger;
    private readonly IWebHostEnvironment _entorno;

    public ManejadorExcepciones(
        RequestDelegate siguiente,
        ILogger<ManejadorExcepciones> logger,
        IWebHostEnvironment entorno)
        => (_siguiente, _logger, _entorno) = (siguiente, logger, entorno);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _siguiente(ctx);
        }
        catch (OperationCanceledException)
        {
            // El cliente cerro la pestana a mitad de la peticion. No es
            // un error del sistema y registrarlo solo ensucia el log.
            if (!ctx.Response.HasStarted)
                ctx.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error no controlado en {Metodo} {Ruta}",
                ctx.Request.Method, ctx.Request.Path);

            if (ctx.Response.HasStarted)
            {
                // La respuesta ya empezo a enviarse: no se puede
                // cambiar el codigo ni el cuerpo sin corromperla.
                return;
            }

            ctx.Response.Clear();
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            // En desarrollo se incluye el detalle para poder depurar.
            // En produccion, nunca: el mensaje de una excepcion puede
            // contener la cadena de conexion o la consulta que fallo.
            var cuerpo = _entorno.IsDevelopment()
                ? new
                {
                    mensaje = "Ocurrió un error inesperado.",
                    detalle = ex.Message,
                    tipo = ex.GetType().Name
                }
                : new
                {
                    mensaje = "Ocurrió un error inesperado. Intente de nuevo.",
                    detalle = (string?)null,
                    tipo = (string?)null
                };

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(cuerpo,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}
