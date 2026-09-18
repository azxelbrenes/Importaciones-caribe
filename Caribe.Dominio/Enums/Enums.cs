namespace Caribe.Dominio.Enums;

/// <summary>
/// Estados por los que pasa un vehiculo.
///
/// El orden importa: los valores se guardan como short en la base,
/// asi que cambiarlos rompe los datos existentes. Para agregar uno
/// nuevo, se usa el siguiente numero libre.
/// </summary>
public enum EstadoVehiculo : short
{
    /// <summary>Cargado pero sin publicar. No aparece en el catalogo.</summary>
    Borrador = 0,

    /// <summary>Publicado y disponible.</summary>
    Disponible = 1,

    /// <summary>Hay un cliente negociando. Sigue visible, con aviso.</summary>
    EnTrato = 2,

    /// <summary>Comprado y en camino a Costa Rica.</summary>
    EnTransito = 3,

    /// <summary>Entregado al cliente.</summary>
    Vendido = 4,

    /// <summary>Fuera del catalogo pero conservado como historial.</summary>
    Archivado = 5
}

public enum Transmision : short
{
    Automatica = 0,
    Manual = 1
}

public enum Combustible : short
{
    Gasolina = 0,
    Diesel = 1,
    Hibrido = 2,
    Electrico = 3
}

public enum Traccion : short
{
    DosPorCuatro = 0,
    CuatroPorCuatro = 1,
    Integral = 2
}

/// <summary>
/// Etapas de una solicitud de cliente.
///
/// Archivada se agrego para poder limpiar la bandeja sin borrar:
/// las solicitudes son el historial comercial y alimentan las
/// estadisticas de conversion.
/// </summary>
public enum EstadoSolicitud : short
{
    Nueva = 0,
    Contactada = 1,
    EnProceso = 2,
    Cerrada = 3,
    Descartada = 4,

    /// <summary>Fuera de la bandeja, pero conservada.</summary>
    Archivada = 5
}

/// <summary>
/// De donde llego la solicitud. Sirve para saber que parte del sitio
/// convierte mejor: si la mayoria viene de "sin resultados", hay
/// demanda de vehiculos que no estan en el catalogo.
/// </summary>
public enum OrigenSolicitud : short
{
    Inicio = 0,
    FormularioCompleto = 1,
    FichaVehiculo = 2,
    PaginaNoEncontrada = 3
}

/// <summary>
/// SuperAdministrador no esta: ese rol es del propietario y se crea
/// con el seed inicial, no se delega por invitacion.
/// </summary>
public enum RolInvitacion : short
{
    Administrador = 1,
    Operador = 2
}

public enum AccionAuditoria : short
{
    Crear = 0,
    Editar = 1,
    Eliminar = 2,
    Acceso = 3,

    /// <summary>Cambio de estado de un vehiculo o solicitud.</summary>
    CambioEstado = 4
}
public enum FormaPago : short
{
    /// <summary>Todavia no lo definio.</summary>
    PorDefinir = 0,

    Contado = 1,

    Financiado = 2
}
