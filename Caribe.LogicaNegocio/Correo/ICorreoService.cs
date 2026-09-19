namespace Caribe.LogicaNegocio.Correo;

/// <summary>
/// Abstrae COMO se envia el correo. La logica de negocio solo pide
/// enviar; no sabe si va por Resend o a la consola.
/// </summary>
public interface ICorreoService
{
    Task<bool> EnviarAsync(
        string destinatario, string asunto, string cuerpoHtml,
        CancellationToken ct = default);

    /// <summary>
    /// Si los correos salen de verdad.
    ///
    /// Existe porque CorreoConsola devuelve true al "enviar", asi que
    /// el resultado del envio no alcanza para saberlo. El panel lo usa
    /// para decidir si muestra el enlace en pantalla o confia en que
    /// llego al buzon.
    /// </summary>
    bool EnvioReal { get; }
}
