using Microsoft.Extensions.Logging;

namespace Caribe.LogicaNegocio.Correo;

/// <summary>
/// Escribe el correo en el registro en vez de enviarlo.
///
/// Permite desarrollar sin depender de un dominio verificado: el
/// enlace aparece en la consola y se puede copiar para probar el
/// flujo completo.
/// </summary>
public class CorreoConsola : ICorreoService
{
    private readonly ILogger<CorreoConsola> _logger;

    public CorreoConsola(ILogger<CorreoConsola> logger) => _logger = logger;

    public bool EnvioReal => false;

    public Task<bool> EnviarAsync(
        string destinatario, string asunto, string cuerpoHtml,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n═══════════ CORREO (no enviado) ═══════════\n" +
            "Para:    {Destinatario}\n" +
            "Asunto:  {Asunto}\n" +
            "───────────────────────────────────────────\n" +
            "{Cuerpo}\n" +
            "═══════════════════════════════════════════",
            destinatario, asunto, SoloTexto(cuerpoHtml));

        return Task.FromResult(true);
    }

    /// <summary>Quita las etiquetas para que el registro sea legible.</summary>
    private static string SoloTexto(string html) =>
        System.Text.RegularExpressions.Regex
            .Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ")
            .Replace("  ", " ")
            .Trim();
}
