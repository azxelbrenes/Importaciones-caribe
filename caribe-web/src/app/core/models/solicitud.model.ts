/// Espejo de SolicitudDto. asignadaA es el id del usuario.
export interface Solicitud {
  id: number;
  nombre: string;
  whatsapp: string;
  marcaTexto: string | null;
  modeloTexto: string | null;
  anioDesde: number | null;
  presupuestoMin: number | null;
  presupuestoMax: number | null;
  formaPago: number;
  formaPagoTexto: string;
  plazoMesesInteres: number | null;
  origen: number;
  origenTexto: string;
  estado: number;
  asignadaA: string | null;
  vehiculoSlug: string | null;
  creadoEn: string;
  atendidaEn: string | null;
  cantidadNotas: number;
}

export interface Nota {
  id: number;
  usuarioId: string;
  nota: string;
  creadoEn: string;
}

export interface SolicitudDetalle extends Omit<Solicitud, 'cantidadNotas'> {
  transmision: number | null;
  combustible: number | null;
  detalles: string | null;
  notas: Nota[];
}

export interface FiltroSolicitud {
  estado?: number;
  busqueda?: string;
  incluirArchivadas?: boolean;
  pagina?: number;
  porPagina?: number;
}

/// Financiado vale 2 en el enum FormaPago del backend.
export const FORMA_PAGO_FINANCIADO = 2;
