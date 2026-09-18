using Caribe.Dominio.Enums;

namespace Caribe.Utilitarios;

/// <summary>
/// Que cambios de estado estan permitidos.
///
/// Sin esto, un vehiculo podria pasar de Vendido a Disponible y
/// descuadrar los reportes de ventas del mes. La maquina de estados
/// evita esos saltos.
///
/// El frontend replica esta tabla para no ofrecer opciones que el
/// servidor va a rechazar, pero la validacion real vive aqui.
/// </summary>
public static class TransicionEstado
{
    private static readonly Dictionary<EstadoVehiculo, EstadoVehiculo[]> Vehiculos = new()
    {
        [EstadoVehiculo.Borrador] =
            [EstadoVehiculo.Disponible, EstadoVehiculo.Archivado],

        [EstadoVehiculo.Disponible] =
            [EstadoVehiculo.EnTrato, EstadoVehiculo.EnTransito,
             EstadoVehiculo.Vendido, EstadoVehiculo.Borrador,
             EstadoVehiculo.Archivado],

        [EstadoVehiculo.EnTrato] =
            [EstadoVehiculo.Disponible, EstadoVehiculo.EnTransito,
             EstadoVehiculo.Vendido],

        [EstadoVehiculo.EnTransito] =
            [EstadoVehiculo.Vendido, EstadoVehiculo.Disponible],

        // Vendido solo puede archivarse: volver atras falsearia
        // los ingresos de un mes ya cerrado.
        [EstadoVehiculo.Vendido] =
            [EstadoVehiculo.Archivado],

        [EstadoVehiculo.Archivado] =
            [EstadoVehiculo.Borrador]
    };

    public static bool EsValida(EstadoVehiculo actual, EstadoVehiculo nuevo) =>
        actual != nuevo
        && Vehiculos.TryGetValue(actual, out var permitidos)
        && permitidos.Contains(nuevo);

    public static IReadOnlyList<EstadoVehiculo> Desde(EstadoVehiculo actual) =>
        Vehiculos.TryGetValue(actual, out var permitidos) ? permitidos : [];
}
