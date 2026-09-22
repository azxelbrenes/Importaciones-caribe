/// Espejo de VehiculoDetalleDto: la ficha pública.
export interface VehiculoDetalle {
  id: number;
  slug: string;
  marca: string;
  modelo: string;
  anio: number;
  kilometraje: number;
  transmision: string;
  combustible: string;
  traccion: string;
  color: string | null;
  descripcion: string | null;
  precioPublicado: number;
  estado: number;
  desglose: DesglosePrecio;
  fotos: Foto[];

  /// Null si el vehículo no se financia o el financiamiento está apagado.
  financiamiento: FinanciamientoVehiculo | null;
}

/// El honorario NO viene por separado: llega sumado dentro de
/// "trámites". La ganancia del negocio no sale al sitio.
export interface DesglosePrecio {
  vehiculo: number;
  flete: number;
  impuestos: number;
  tramites: number;
  total: number;
  vigenciaDias: number;
}

export interface Foto {
  url: string;
  urlThumb: string;
  orden: number;
  esPortada: boolean;
}

export interface FinanciamientoVehiculo {
  porcentajePrima: number;
  prima: number;
  plazos: number[];
}

/// Lo que se envía al tocar "Me interesa".
export interface CrearSolicitud {
  nombre: string;
  whatsapp: string;
  marcaTexto?: string;
  modeloTexto?: string;
  detalles?: string;
  vehiculoId?: number;

  /// 0 Por definir · 1 Contado · 2 Financiado
  formaPago: number;
  plazoMesesInteres?: number;

  /// 2 = Ficha de vehículo, en el enum del backend.
  origen: number;

  /// Exigido por la Ley 8968: sin esto el backend rechaza la solicitud.
  consentimiento: boolean;
}

export const FormaPago = { PorDefinir: 0, Contado: 1, Financiado: 2 } as const;
export const ORIGEN_FICHA = 2;
