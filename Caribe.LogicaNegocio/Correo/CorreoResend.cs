using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caribe.LogicaNegocio.Correo;

/// <summary>
/// Envio real por Resend.
///
/// Requiere dominio verificado con SPF y DKIM: sin esos registros los
/// correos caen en spam o directamente no salen. No es un capricho de
/// Resend, es como funciona el correo hoy.
/// </summary>
public class CorreoResend : ICorreoService
{
    private readonly HttpClient _http;
    private readonly CorreoOpciones _op;
    private readonly ILogger<CorreoResend> _logger;

    public CorreoResend(
        HttpClient http,
        IOptions<CorreoOpciones> op,
        ILogger<CorreoResend> logger)
    {
        _op = op.Value;
        _logger = logger;

        _http = http;
        _http.BaseAddress = new Uri("https://api.resend.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _op.ApiKey);

        // Sin tiempo limite, un fallo de red dejaria la peticion
        // colgada y con ella la operacion que la disparo.
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public bool EnvioReal => true;

    public async Task<bool> EnviarAsync(
        string destinatario, string asunto, string cuerpoHtml,
        CancellationToken ct = default)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("emails", new
            {
                from = $"{_op.NombreRemitente} <{_op.Remitente}>",
                to = new[] { destinatario },
                subject = asunto,
                html = cuerpoHtml
            }, ct);

            if (r.IsSuccessStatusCode) return true;

            var detalle = await r.Content.ReadAsStringAsync(ct);

            _logger.LogError(
                "Resend respondió {Codigo} al enviar a {Destinatario}: {Detalle}",
                (int)r.StatusCode, destinatario, detalle);

            return false;
        }
        catch (Exception ex)
        {
            // Un fallo de correo NO debe tumbar la operacion que lo
            // genero: la invitacion ya quedo creada y se puede
            // reenviar, pero si esto lanzara, el usuario veria un 500
            // y pensaria que nada funciono.
            _logger.LogError(ex, "Error enviando correo a {Destinatario}", destinatario);
            return false;
        }
    }
}
