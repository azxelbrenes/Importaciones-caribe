export interface ConfiguracionFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
  actualizadoEn: string;
}

export interface GuardarFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
}

/// Lo que ve el sitio: si se ofrece, la prima y los plazos. Sin
/// tasas: el interés lo da el dueño por WhatsApp.
export interface FinanciamientoPublico {
  activo: boolean;
  porcentajePrima: number;
  plazos: number[];
}