export interface Marca {
  id: number;
  nombre: string;
  activa: boolean;
  cantidadModelos: number;

  /// Cuantos vehiculos la usan. Si es mayor a cero, no se puede
  /// eliminar: solo desactivar.
  cantidadVehiculos: number;
}

export interface Modelo {
  id: number;
  marcaId: number;
  marca: string;
  nombre: string;
  activo: boolean;
  cantidadVehiculos: number;
}

export interface Limpieza {
  marcasFusionadas: number;
  modelosFusionados: number;
  nombresCorregidos: number;
}

/// Opcion de un desplegable. Viene del backend para que los textos
/// vivan en un solo lugar.
export interface Opcion {
  valor: number;
  texto: string;
}
