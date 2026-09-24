export interface ConfiguracionFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  /// Una sola vez sobre lo financiado. Solo llega al panel, nunca al sitio.
  porcentajeInteres: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
  actualizadoEn: string;
}

export interface GuardarFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  porcentajeInteres: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
}

/// Lo que ve el sitio: si se ofrece, la prima y los plazos. Nunca el
/// interés: las cuotas vienen ya calculadas en cada ficha.
export interface FinanciamientoPublico {
  activo: boolean;
  porcentajePrima: number;
  plazos: number[];
}