using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Caribe.Utilitarios;

/// <summary>
/// Genera identificadores para URL a partir de texto.
///
/// "Toyota Tacoma TRD Pro 2023" → "toyota-tacoma-trd-pro-2023"
///
/// Se usa en las direcciones publicas de los vehiculos: un slug es
/// legible para quien comparte el enlace por WhatsApp y le da a los
/// buscadores palabras que indexar, cosa que un id numerico no hace.
/// </summary>
public static partial class Slug
{
    public static string Generar(params string?[] partes)
    {
        var texto = string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        // Se separan los acentos de sus letras y se descartan: "ñ"
        // se vuelve "n", "á" se vuelve "a". Sin esto, una URL con
        // acentos se codifica y queda ilegible al compartirla.
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var limpio = sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        limpio = NoAlfanumerico().Replace(limpio, "-");
        limpio = GuionesRepetidos().Replace(limpio, "-");

        return limpio.Trim('-');
    }

    /// <summary>
    /// Agrega un sufijo numerico si el slug ya existe.
    /// Dos Tacoma 2023 del mismo color son posibles, y las URL no
    /// pueden repetirse.
    /// </summary>
    public static string ConSufijo(string slug, int numero) =>
        numero <= 1 ? slug : $"{slug}-{numero}";

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NoAlfanumerico();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex GuionesRepetidos();
}
