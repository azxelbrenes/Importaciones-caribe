/// Espejo de VehiculoDto: lo que ve el visitante en el catálogo.
///
/// No tiene costos ni margen. No es que se oculten: el backend nunca
/// los manda en este tipo.
export interface VehiculoPublico {
  slug: string;
  marca: string;
  modelo: string;
  anio: number;
  kilometraje: number;
  color: string | null;
  precioPublicado: number;
  estado: number;
  destacado: boolean;
  aceptaFinanciamiento: boolean;
  fotoPortada: string | null;
}

/// En trato vale 2 en el enum del backend: sigue a la venta, pero ya
/// hay alguien negociándolo.
export const ESTADO_EN_TRATO = 2;

export interface FiltroCatalogo {
  marcaId?: number;
  modeloId?: number;
  anioDesde?: number;
  precioMax?: number;
  transmision?: number;
  combustible?: number;
  aceptaFinanciamiento?: boolean;
  busqueda?: string;
  ordenarPor?: string;
  pagina?: number;
  porPagina?: number;
}

export const Ordenes = [
  { valor: 'reciente',    texto: 'Más recientes' },
  { valor: 'precio_asc',  texto: 'Menor precio' },
  { valor: 'precio_desc', texto: 'Mayor precio' },
  { valor: 'km_asc',      texto: 'Menos kilometraje' },
  { valor: 'anio_desc',   texto: 'Más nuevos' }
];
