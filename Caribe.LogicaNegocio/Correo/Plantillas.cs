using System.Net;

namespace Caribe.LogicaNegocio.Correo;

/// <summary>
/// Plantillas HTML de los correos, con la identidad del sitio.
///
/// Se escriben con tablas y estilos en linea, no con CSS moderno: los
/// clientes de correo —Outlook sobre todo— ignoran las hojas de
/// estilo, las variables, flexbox y grid. Es HTML de hace veinte anos
/// a proposito, y es la unica forma de que se vea igual en Gmail, en
/// Outlook y en el celular.
///
/// Por eso tampoco hay tipografias propias: Gmail no las carga. Se usa
/// Arial, y el caracter de marca sale de las mayusculas espaciadas y
/// de los colores.
/// </summary>
public static class Plantillas
{
    // Mismos colores que el sitio.
    private const string Noche = "#05070A";
    private const string Azul = "#1580E8";
    private const string Rojo = "#E01E2B";
    private const string Verde = "#1FA97F";
    private const string Ambar = "#B7861C";
    private const string Texto = "#1B2330";
    private const string Suave = "#4A5563";
    private const string Tenue = "#8A94A3";
    private const string Fondo = "#EEF1F4";
    private const string Caja = "#F5F7F9";

    /// <summary>
    /// URL absoluta: un correo no puede usar rutas relativas, no tiene
    /// un "sitio" desde el cual resolverlas. Se sirve desde la carpeta
    /// public del frontend.
    /// </summary>
    private const string Logo = "https://importacionescaribecr.com/logo.jpg";
    private const string Sitio = "https://importacionescaribecr.com";

    private const string Fuente = "Arial,Helvetica,sans-serif";

    // ══════════════════ PLANTILLAS ══════════════════

    public static string Invitacion(string enlace, string rol, string invitadoPor) =>
        Base(
            vistaPrevia: $"{invitadoPor} te dio acceso al panel como {rol}.",
            etiqueta: "Invitación",
            colorEtiqueta: Azul,
            titulo: "Te dieron acceso al panel",
            cuerpo: Parrafo(
                        $"<strong style=\"color:{Texto};\">{E(invitadoPor)}</strong> te " +
                        "invitó a administrar el sitio de Importaciones del Caribe CR.")
                  + Recuadro(Azul,
                        Dato("Tu rol", E(rol)) +
                        Dato("Vence", "en 72 horas"))
                  + Parrafo("Para activar tu cuenta, creá tu contraseña con el botón:"),
            textoBoton: "Activar mi cuenta",
            enlace: enlace,
            pie: "El enlace sirve una sola vez. Si no esperabas esta invitación, " +
                 "ignorá este correo: sin activarla, no se crea ninguna cuenta.");

    public static string RecuperarPassword(string enlace, string nombre, int minutos) =>
        Base(
            vistaPrevia: $"Enlace para crear una contraseña nueva. Vence en {minutos} minutos.",
            etiqueta: "Seguridad",
            colorEtiqueta: Azul,
            titulo: "Restablecer tu contraseña",
            cuerpo: Parrafo($"Hola {E(Primero(nombre))},")
                  + Parrafo("Recibimos un pedido para restablecer la contraseña de tu " +
                            "cuenta. Si fuiste vos, creá una nueva con el botón:"),
            textoBoton: "Crear contraseña nueva",
            enlace: enlace,
            // El aviso importa: si alguien recibe esto sin haberlo
            // pedido, otra persona conoce su correo y esta intentando
            // entrar.
            nota: Aviso(Ambar,
                  $"El enlace vence en <strong>{minutos} minutos</strong> y sirve una vez. " +
                  "Si no pediste este cambio, ignorá el correo: tu contraseña sigue igual."),
            pie: "Por seguridad, nunca te vamos a pedir tu contraseña por correo ni por WhatsApp.");

    public static string PasswordCambiada(string nombre) =>
        Base(
            vistaPrevia: "Tu contraseña se cambió y se cerraron todas las sesiones.",
            etiqueta: "Aviso de seguridad",
            colorEtiqueta: Verde,
            titulo: "Tu contraseña cambió",
            cuerpo: Parrafo($"Hola {E(Primero(nombre))},")
                  + Parrafo("La contraseña de tu cuenta se cambió correctamente, y " +
                            "todas las sesiones abiertas se cerraron."),
            textoBoton: null,
            enlace: null,
            nota: Aviso(Rojo,
                  "<strong>¿No fuiste vos?</strong> Avisá de inmediato al administrador: " +
                  "alguien más tiene acceso a tu cuenta."),
            pie: "Este es un aviso automático. Lo mandamos cada vez que la contraseña cambia.");

    public static string SolicitudNueva(
        string nombreCliente, string whatsapp, string vehiculo, string enlace) =>
        Base(
            vistaPrevia: $"{nombreCliente} consultó por {vehiculo}.",
            etiqueta: "Nueva solicitud",
            colorEtiqueta: Rojo,
            titulo: "Un cliente está interesado",
            cuerpo: Recuadro(Rojo,
                        Dato("Cliente", E(nombreCliente)) +
                        Dato("WhatsApp", E(whatsapp)) +
                        Dato("Vehículo", E(vehiculo))),
            textoBoton: "Ver en el panel",
            enlace: enlace,
            pie: "Responder en la primera hora es lo que más aumenta la probabilidad " +
                 "de cerrar la venta.");

    // ══════════════════ PIEZAS ══════════════════

    private static string Parrafo(string html) =>
        $"<p style=\"margin:0 0 16px;font-family:{Fuente};font-size:15px;" +
        $"line-height:1.6;color:{Suave};\">{html}</p>";

    /// <summary>Caja gris con franja de color, para datos clave.</summary>
    private static string Recuadro(string color, string filas) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
               style="margin:4px 0 22px;background:{Caja};border-left:3px solid {color};">
          <tr><td style="padding:14px 18px;">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%">
              {filas}
            </table>
          </td></tr>
        </table>
        """;

    private static string Dato(string etiqueta, string valor) => $"""
        <tr>
          <td style="padding:5px 0;font-family:{Fuente};font-size:11px;letter-spacing:1.5px;
                     text-transform:uppercase;color:{Tenue};width:96px;vertical-align:top;">
            {etiqueta}
          </td>
          <td style="padding:5px 0;font-family:{Fuente};font-size:15px;font-weight:bold;
                     color:{Texto};">
            {valor}
          </td>
        </tr>
        """;

    private static string Aviso(string color, string html) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
               style="margin:6px 0 0;">
          <tr><td style="padding:12px 16px;border:1px solid {color};border-left:3px solid {color};
                         font-family:{Fuente};font-size:13px;line-height:1.55;color:{Suave};">
            {html}
          </td></tr>
        </table>
        """;

    /// <summary>
    /// Boton "a prueba de balas": una celda con fondo y el enlace
    /// adentro. Un &lt;button&gt; o un enlace con padding no se ven igual
    /// en Outlook, que ignora el padding de los enlaces.
    /// </summary>
    private static string Boton(string texto, string enlace) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0"
               style="margin:6px 0 22px;">
          <tr>
            <td style="background:{Rojo};">
              <a href="{enlace}" target="_blank"
                 style="display:inline-block;padding:15px 30px;font-family:{Fuente};
                        font-size:14px;font-weight:bold;letter-spacing:1.5px;
                        text-transform:uppercase;color:#ffffff;text-decoration:none;">
                {texto} &rarr;
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0 0 22px;font-family:{Fuente};font-size:12px;line-height:1.5;
                  color:{Tenue};word-break:break-all;">
          ¿El botón no funciona? Copiá esta dirección en el navegador:<br>
          <a href="{enlace}" style="color:{Azul};text-decoration:underline;">{enlace}</a>
        </p>
        """;

    // ══════════════════ ARMADO ══════════════════

    private static string Base(
        string vistaPrevia, string etiqueta, string colorEtiqueta, string titulo,
        string cuerpo, string? textoBoton, string? enlace, string pie, string? nota = null)
    {
        var boton = textoBoton is not null && enlace is not null
            ? Boton(textoBoton, enlace)
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <!-- Se declara claro: algunos clientes invierten los colores
                   en modo oscuro y dejan el texto ilegible. -->
              <meta name="color-scheme" content="light">
              <meta name="supported-color-schemes" content="light">
              <title>{E(titulo)}</title>
            </head>
            <body style="margin:0;padding:0;background:{Fondo};">

              <!-- Texto de vista previa: lo que Gmail muestra junto al
                   asunto en la bandeja. Invisible dentro del correo. -->
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;
                          color:transparent;font-size:1px;line-height:1px;">
                {E(vistaPrevia)}&#8203;&nbsp;&#8203;&nbsp;&#8203;&nbsp;&#8203;&nbsp;
              </div>

              <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                     style="background:{Fondo};">
                <tr>
                  <td align="center" style="padding:32px 14px;">

                    <table role="presentation" cellpadding="0" cellspacing="0" border="0"
                           width="100%" style="max-width:560px;">

                      <!-- Franja de los dos países -->
                      <tr>
                        <td style="height:4px;line-height:4px;font-size:0;
                                   background:{Azul};
                                   background-image:linear-gradient(90deg,{Azul},#ffffff,{Rojo});">
                          &nbsp;
                        </td>
                      </tr>

                      <!-- Encabezado -->
                      <tr>
                        <td style="background:{Noche};padding:22px 32px;">
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td style="padding-right:14px;vertical-align:middle;">
                                <img src="{Logo}" width="48" height="48"
                                     alt="Importaciones del Caribe CR"
                                     style="display:block;border:0;width:48px;height:48px;">
                              </td>
                              <td style="vertical-align:middle;">
                                <p style="margin:0;font-family:{Fuente};font-size:16px;
                                          font-weight:bold;letter-spacing:2px;
                                          text-transform:uppercase;color:#ffffff;">
                                  Importaciones del Caribe
                                </p>
                                <p style="margin:3px 0 0;font-family:{Fuente};font-size:10px;
                                          letter-spacing:3px;text-transform:uppercase;
                                          color:#8A99AB;">
                                  De USA a Costa Rica
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Contenido -->
                      <tr>
                        <td style="background:#ffffff;padding:34px 32px 12px;">
                          <p style="margin:0 0 10px;font-family:{Fuente};font-size:11px;
                                    font-weight:bold;letter-spacing:2px;text-transform:uppercase;
                                    color:{colorEtiqueta};">
                            {etiqueta}
                          </p>
                          <h1 style="margin:0 0 22px;font-family:{Fuente};font-size:26px;
                                     line-height:1.2;font-weight:bold;color:{Texto};">
                            {E(titulo)}
                          </h1>
                          {cuerpo}
                          {boton}
                          {nota ?? string.Empty}
                        </td>
                      </tr>

                      <!-- Pie de la tarjeta -->
                      <tr>
                        <td style="background:#ffffff;padding:18px 32px 30px;">
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0"
                                 width="100%" style="border-top:1px solid #E3E7EC;">
                            <tr><td style="padding-top:18px;font-family:{Fuente};font-size:12px;
                                           line-height:1.6;color:{Tenue};">
                              {pie}
                            </td></tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Pie general -->
                      <tr>
                        <td align="center" style="padding:22px 20px 0;font-family:{Fuente};
                                                  font-size:11px;line-height:1.6;color:{Tenue};">
                          <a href="{Sitio}" style="color:{Suave};text-decoration:none;
                                                   font-weight:bold;letter-spacing:1px;">
                            importacionescaribecr.com
                          </a><br>
                          Correo automático del panel. Esta dirección no recibe respuestas.
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Escapa lo que escribio un usuario. El nombre de un cliente llega
    /// desde el formulario publico: sin esto, alguien podria poner
    /// etiquetas en su nombre y se ejecutarian en el correo del dueno.
    /// </summary>
    private static string E(string texto) => WebUtility.HtmlEncode(texto);

    /// <summary>"Carlos Jiménez Rojas" → "Carlos". Más cercano en el saludo.</summary>
    private static string Primero(string nombre) =>
        nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
}