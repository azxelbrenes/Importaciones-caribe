export interface ConfiguracionFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  tasaAnual: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
  textoLegal: string | null;
  estaOperativo: boolean;
  actualizadoEn: string;
}

export interface GuardarFinanciamiento {
  activo: boolean;
  porcentajePrima: number;
  tasaAnual: number;
  plazoMinimoMeses: number;
  plazoMaximoMeses: number;
  plazosDisponibles: string;
  textoLegal: string | null;
}
