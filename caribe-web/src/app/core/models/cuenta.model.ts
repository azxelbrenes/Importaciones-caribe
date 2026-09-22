export interface Perfil {
  nombreCompleto: string;
  email: string;
  roles: string[];
  dobleFactorActivo: boolean;
  ultimoAcceso: string | null;
  creadoEn: string;
}

export interface ConfigDobleFactor {
  /// Agrupada de 4 en 4 para poder copiarla a mano sin errores.
  claveManual: string;

  /// otpauth://… — es lo que se convierte en codigo QR.
  uriAutenticador: string;
}

export interface Sesion {
  id: number;
  ip: string | null;
  creadoEn: string;
  expiraEn: string;
  esLaActual: boolean;
}
