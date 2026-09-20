export interface Marca {
  id: number;
  nombre: string;
  activa: boolean;
  cantidadModelos: number;
}

export interface Modelo {
  id: number;
  marcaId: number;
  marca: string;
  nombre: string;
  activo: boolean;
}

/// Opcion de un desplegable. Viene del backend para que los textos
/// vivan en un solo lugar: si el servidor dice "En tránsito" y el
/// frontend "En transito", el cliente pregunta si son cosas distintas.
export interface Opcion {
  valor: number;
  texto: string;
}
