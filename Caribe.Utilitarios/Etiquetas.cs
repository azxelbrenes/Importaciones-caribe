using Caribe.Dominio.Enums;

namespace Caribe.Utilitarios;

/// <summary>
/// Textos en espanol de los enums.
///
/// Viven aqui y no en el frontend para que la API y el panel usen
/// exactamente las mismas palabras: si el sitio dice "En tránsito"
/// y un reporte dice "EnTransito", el cliente pregunta si son cosas
/// distintas.
/// </summary>
public static class Etiquetas
{
    public static string De(EstadoVehiculo estado) => estado switch
    {
        EstadoVehiculo.Borrador   => "Borrador",
        EstadoVehiculo.Disponible => "Disponible",
        EstadoVehiculo.EnTrato    => "En trato",
        EstadoVehiculo.EnTransito => "En tránsito",
        EstadoVehiculo.Vendido    => "Vendido",
        EstadoVehiculo.Archivado  => "Archivado",
        _ => estado.ToString()
    };

    public static string De(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Nueva      => "Nueva",
        EstadoSolicitud.Contactada => "Contactada",
        EstadoSolicitud.EnProceso  => "En proceso",
        EstadoSolicitud.Cerrada    => "Cerrada",
        EstadoSolicitud.Descartada => "Descartada",
        EstadoSolicitud.Archivada  => "Archivada",
        _ => estado.ToString()
    };

    public static string De(Transmision t) => t switch
    {
        Transmision.Automatica => "Automática",
        Transmision.Manual     => "Manual",
        _ => t.ToString()
    };

    public static string De(Combustible c) => c switch
    {
        Combustible.Gasolina  => "Gasolina",
        Combustible.Diesel    => "Diésel",
        Combustible.Hibrido   => "Híbrido",
        Combustible.Electrico => "Eléctrico",
        _ => c.ToString()
    };

    public static string De(Traccion t) => t switch
    {
        Traccion.DosPorCuatro    => "4x2",
        Traccion.CuatroPorCuatro => "4x4",
        Traccion.Integral        => "AWD",
        _ => t.ToString()
    };

    public static string De(OrigenSolicitud o) => o switch
    {
        OrigenSolicitud.Inicio             => "Inicio",
        OrigenSolicitud.FormularioCompleto => "Formulario",
        OrigenSolicitud.FichaVehiculo      => "Ficha de vehículo",
        OrigenSolicitud.PaginaNoEncontrada => "Página no encontrada",
        _ => o.ToString()
    };

    public static string De(FormaPago f) => f switch
    {
        FormaPago.PorDefinir => "Por definir",
        FormaPago.Contado    => "Contado",
        FormaPago.Financiado => "Financiado",
        _ => f.ToString()
    };

    public static string De(RolInvitacion r) => r switch
    {
        RolInvitacion.Administrador => "Administrador",
        RolInvitacion.Operador      => "Operador",
        _ => r.ToString()
    };
}
