namespace Caribe.LogicaNegocio.Correo;

/// <summary>
/// Plantillas HTML de los correos.
///
/// Se escriben con tablas y estilos en linea, no con CSS moderno:
/// los clientes de correo —sobre todo Outlook— ignoran gran parte de
/// las hojas de estilo. Es HTML de hace veinte anos a proposito.
/// </summary>
public static class Plantillas
{
    private const string Azul = "#1580E8";
    private const string Rojo = "#E01E2B";
    private const string Texto = "#1a1a1a";
    private const string Tenue = "#666666";

    public static string Invitacion(string enlace, string rol, string invitadoPor) =>
        Base(
            titulo: "Le invitaron al panel",
            cuerpo: $"""
                <p style="margin:0 0 16px;font-size:15px;color:{Texto};">
                  <strong>{Escapar(invitadoPor)}</strong> le dio acceso al panel
                  administrativo de Importaciones del Caribe CR como
                  <strong>{Escapar(rol)}</strong>.
                </p>
                <p style="margin:0 0 24px;font-size:15px;color:{Texto};">
                  Para activar su cuenta y crear su contraseña, haga clic
                  en el botón:
                </p>
                """,
            textoBoton: "Activar mi cuenta",
            enlace: enlace,
            pie: "Este enlace vence en 72 horas y solo puede usarse una vez. " +
                 "Si no esperaba esta invitación, ignore este mensaje.");

    public static string RecuperarPassword(string enlace, string nombre, int minutos) =>
        Base(
            titulo: "Restablecer su contraseña",
            cuerpo: $"""
                <p style="margin:0 0 16px;font-size:15px;color:{Texto};">
                  Hola {Escapar(nombre)},
                </p>
                <p style="margin:0 0 24px;font-size:15px;color:{Texto};">
                  Recibimos una solicitud para restablecer la contraseña de
                  su cuenta. Si fue usted, haga clic en el botón para crear
                  una nueva:
                </p>
                """,
            textoBoton: "Crear contraseña nueva",
            enlace: enlace,
            // El aviso importa: si alguien recibe esto sin haberlo
            // pedido, significa que otra persona conoce su correo y
            // esta intentando entrar.
            pie: $"Este enlace vence en {minutos} minutos y solo sirve una vez. " +
                 "Si usted no pidió este cambio, ignore este mensaje: su " +
                 "contraseña sigue siendo la misma.");

    public static string PasswordCambiada(string nombre) =>
        Base(
            titulo: "Su contraseña cambió",
            cuerpo: $"""
                <p style="margin:0 0 16px;font-size:15px;color:{Texto};">
                  Hola {Escapar(nombre)},
                </p>
                <p style="margin:0 0 16px;font-size:15px;color:{Texto};">
                  La contraseña de su cuenta se cambió correctamente y
                  todas las sesiones abiertas se cerraron.
                </p>
                <p style="margin:0 0 24px;font-size:15px;color:{Rojo};">
                  <strong>Si usted no hizo este cambio</strong>, avise de
                  inmediato al administrador: alguien más tiene acceso a
                  su cuenta.
                </p>
                """,
            textoBoton: null,
            enlace: null,
            pie: "Este es un aviso automático de seguridad.");

    public static string SolicitudNueva(
        string nombreCliente, string whatsapp, string vehiculo, string enlace) =>
        Base(
            titulo: "Nueva solicitud",
            cuerpo: $"""
                <p style="margin:0 0 16px;font-size:15px;color:{Texto};">
                  <strong>{Escapar(nombreCliente)}</strong> pidió información.
                </p>
                <table cellpadding="0" cellspacing="0" border="0" width="100%"
                       style="margin:0 0 24px;border-collapse:collapse;">
                  <tr>
                    <td style="padding:8px 0;font-size:14px;color:{Tenue};width:110px;">
                      WhatsApp
                    </td>
                    <td style="padding:8px 0;font-size:14px;color:{Texto};">
                      {Escapar(whatsapp)}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:8px 0;font-size:14px;color:{Tenue};">Busca</td>
                    <td style="padding:8px 0;font-size:14px;color:{Texto};">
                      {Escapar(vehiculo)}
                    </td>
                  </tr>
                </table>
                """,
            textoBoton: "Ver en el panel",
            enlace: enlace,
            pie: "Responder pronto aumenta mucho la probabilidad de cerrar la venta.");

    // ══════════════════ ARMADO ══════════════════

    private static string Base(
        string titulo, string cuerpo, string? textoBoton,
        string? enlace, string pie)
    {
        var boton = textoBoton is not null && enlace is not null
            ? $"""
               <table cellpadding="0" cellspacing="0" border="0"
                      style="margin:0 0 24px;">
                 <tr>
                   <td style="background:{Azul};">
                     <a href="{enlace}"
                        style="display:inline-block;padding:14px 28px;
                               font-family:Arial,sans-serif;font-size:15px;
                               font-weight:bold;color:#ffffff;
                               text-decoration:none;">
                       {textoBoton}
                     </a>
                   </td>
                 </tr>
               </table>

               <p style="margin:0 0 24px;font-size:12px;color:{Tenue};
                         word-break:break-all;">
                 Si el botón no funciona, copie esta dirección:<br>
                 {enlace}
               </p>
               """
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>{titulo}</title>
            </head>
            <body style="margin:0;padding:0;background:#f4f5f7;">
              <table cellpadding="0" cellspacing="0" border="0" width="100%"
                     style="background:#f4f5f7;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table cellpadding="0" cellspacing="0" border="0" width="100%"
                           style="max-width:520px;background:#ffffff;
                                  font-family:Arial,Helvetica,sans-serif;">
                      <tr>
                        <td style="height:4px;background:{Azul};"></td>
                      </tr>
                      <tr>
                        <td style="padding:32px 32px 8px;">
                          <p style="margin:0 0 4px;font-size:11px;
                                    letter-spacing:2px;text-transform:uppercase;
                                    color:{Tenue};">
                            Importaciones del Caribe CR
                          </p>
                          <h1 style="margin:0 0 24px;font-size:22px;
                                     color:{Texto};">
                            {titulo}
                          </h1>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:0 32px;">
                          {cuerpo}
                          {boton}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 32px 32px;
                                   border-top:1px solid #e5e7eb;">
                          <p style="margin:16px 0 0;font-size:12px;
                                    color:{Tenue};line-height:1.6;">
                            {pie}
                          </p>
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
    /// Escapa el HTML de lo que escribio un usuario.
    ///
    /// El nombre de un cliente llega desde el formulario publico. Sin
    /// esto, alguien podria poner etiquetas en su nombre y esas
    /// etiquetas se ejecutarian en el correo que abre el dueno.
    /// </summary>
    private static string Escapar(string texto) =>
        System.Net.WebUtility.HtmlEncode(texto);
}
