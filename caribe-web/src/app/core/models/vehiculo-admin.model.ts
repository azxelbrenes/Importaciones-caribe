/// Espejo de VehiculoAdminDto. A diferencia del modelo publico, este
/// SI trae costos y margen: es la vista interna del negocio.
export interface VehiculoAdmin {
  id: number;
  slug: string;
  marca: string;
  modelo: string;
  anio: number;
  kilometraje: number;
  color: string | null;
  costoTotal: number;
  honorario: number;
  precioPublicado: number;
  margen: number;
  estado: number;
  destacado: boolean;
  aceptaFinanciamiento: boolean;
  visitas: number;
  cantidadFotos: number;
  creadoEn: string;
  publicadoEn: string | null;
}

export const EstadoVehiculo = {
  Borrador:   0,
  Disponible: 1,
  EnTrato:    2,
  EnTransito: 3,
  Vendido:    4,
  Archivado:  5
} as const;

export const EtiquetasEstado: Record<number, string> = {
  0: 'Borrador',
  1: 'Disponible',
  2: 'En trato',
  3: 'En tránsito',
  4: 'Vendido',
  5: 'Archivado'
};

/// Clase CSS de la pastilla de cada estado. Vive en _panel.scss.
export const ClasesEstado: Record<number, string> = {
  0: 'borrador',
  1: 'disponible',
  2: 'trato',
  3: 'transito',
  4: 'vendido',
  5: 'archivado'
};

/// La misma tabla que valida el backend en TransicionEstado.
///
/// Se replica aqui para no ofrecer opciones que el servidor va a
/// rechazar: es mejor no mostrar el boton que mostrar un error
/// despues de pulsarlo. La validacion real sigue en el servidor.
export const TransicionesValidas: Record<number, number[]> = {
  0: [1, 5],           // Borrador    → Disponible, Archivado
  1: [2, 3, 4, 0, 5],  // Disponible  → En trato, En tránsito, Vendido, Borrador, Archivado
  2: [1, 3, 4],        // En trato    → Disponible, En tránsito, Vendido
  3: [4, 1],           // En tránsito → Vendido, Disponible
  4: [5],              // Vendido     → solo Archivado
  5: [0]               // Archivado   → Borrador
};

export interface FiltroVehiculoAdmin {
  marcaId?: number;
  estado?: number;
  busqueda?: string;
  aceptaFinanciamiento?: boolean;
  ordenarPor?: string;
  pagina?: number;
  porPagina?: number;
}
