/// Espejo de VehiculoDetalleAdminDto: lo que devuelve
/// GET /api/vehiculos/admin/{id} para llenar el formulario.
export interface VehiculoDetalleAdmin {
  id: number;
  slug: string;
  marcaId: number;
  modeloId: number;
  anio: number;
  kilometraje: number;
  transmision: number;
  combustible: number;
  traccion: number;
  color: string | null;
  descripcion: string | null;
  costoVehiculo: number;
  costoFlete: number;
  costoImpuestos: number;
  costoTramites: number;
  honorario: number;
  precioPublicado: number;
  vigenciaDias: number;
  estado: number;
  destacado: boolean;
  aceptaFinanciamiento: boolean;
}

/// Lo que se envia al crear o actualizar.
///
/// El precio NO viaja: lo calcula el servidor desde los costos. Si
/// viniera del navegador, cualquiera podria publicar un vehiculo a
/// un dolar desde las herramientas de desarrollo.
export interface GuardarVehiculo {
  id?: number;
  marcaId: number;
  modeloId: number;
  anio: number;
  kilometraje: number;
  transmision: number;
  combustible: number;
  traccion: number;
  color?: string;
  descripcion?: string;
  costoVehiculo: number;
  costoFlete: number;
  costoImpuestos: number;
  costoTramites: number;
  honorario: number;
  vigenciaDias: number;
  destacado: boolean;
  aceptaFinanciamiento: boolean;
}

export interface Foto {
  id: number;
  url: string;
  urlThumb: string;
  orden: number;
  esPortada: boolean;
}

/// Respuesta de la subida multiple: si de ocho fotos una esta
/// corrupta, las otras siete entran igual y se informa cual fallo.
export interface ResultadoSubida {
  subidas: Foto[];
  errores: { archivo: string; motivo: string }[];
}
