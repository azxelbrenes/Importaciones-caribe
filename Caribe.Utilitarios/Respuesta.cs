namespace Caribe.Utilitarios;

/// <summary>
/// Tipo de fallo de una operacion de negocio.
///
/// Existe para que el controlador traduzca cada caso al codigo HTTP
/// correcto sin conocer las reglas: un vehiculo inexistente es 404,
/// uno sin fotos que se intenta publicar es 409, y los dos son
/// "fallo" desde la logica.
/// </summary>
public enum TipoError
{
    Ninguno = 0,

    /// <summary>El recurso no existe. → 404</summary>
    NoEncontrado = 1,

    /// <summary>Los datos enviados no sirven. → 400</summary>
    Validacion = 2,

    /// <summary>
    /// El estado actual impide la operacion. → 409
    /// Publicar sin fotos, invitar un correo que ya tiene cuenta.
    /// </summary>
    Conflicto = 3,

    /// <summary>Autenticado pero sin permiso. → 403</summary>
    SinPermiso = 4
}

/// <summary>
/// Resultado de una operacion de negocio.
///
/// Se usa en vez de lanzar excepciones para los fallos esperados.
/// Una excepcion es para lo que no deberia pasar; "este correo ya
/// esta registrado" es un caso previsto y tiene que ser barato de
/// manejar, no costar una traza de pila.
/// </summary>
public class Respuesta<T>
{
    public bool Exitoso { get; private init; }
    public T? Valor { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;
    public TipoError Error { get; private init; }

    private Respuesta() { }

    public static Respuesta<T> Ok(T valor) => new()
    {
        Exitoso = true,
        Valor = valor,
        Error = TipoError.Ninguno
    };

    public static Respuesta<T> NoEncontrado(string mensaje) => new()
    {
        Exitoso = false,
        Mensaje = mensaje,
        Error = TipoError.NoEncontrado
    };

    public static Respuesta<T> Invalido(string mensaje) => new()
    {
        Exitoso = false,
        Mensaje = mensaje,
        Error = TipoError.Validacion
    };

    public static Respuesta<T> Conflicto(string mensaje) => new()
    {
        Exitoso = false,
        Mensaje = mensaje,
        Error = TipoError.Conflicto
    };

    public static Respuesta<T> SinPermiso(string mensaje) => new()
    {
        Exitoso = false,
        Mensaje = mensaje,
        Error = TipoError.SinPermiso
    };

    /// <summary>
    /// Convierte un fallo a otro tipo de resultado, conservando el
    /// motivo. Sirve cuando un metodo llama a otro y quiere propagar
    /// el error sin repetir la clasificacion.
    /// </summary>
    public Respuesta<TOtro> ComoFallo<TOtro>()
    {
        if (Exitoso)
            throw new InvalidOperationException(
                "Solo se convierten respuestas fallidas.");

        return Error switch
        {
            TipoError.NoEncontrado => Respuesta<TOtro>.NoEncontrado(Mensaje),
            TipoError.Conflicto    => Respuesta<TOtro>.Conflicto(Mensaje),
            TipoError.SinPermiso   => Respuesta<TOtro>.SinPermiso(Mensaje),
            _                      => Respuesta<TOtro>.Invalido(Mensaje)
        };
    }
}
