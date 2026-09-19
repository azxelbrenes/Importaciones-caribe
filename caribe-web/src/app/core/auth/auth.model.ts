export interface Token {
  accessToken: string;
  expiraEn: string;
  nombreCompleto: string;
  email: string;
  roles: string[];
}

export interface LoginResultado {
  requiereDobleFactor: boolean;
  token: Token | null;
}

export const Roles = {
  SuperAdministrador: 'SuperAdministrador',
  Administrador: 'Administrador',
  Operador: 'Operador'
} as const;

/// Vehiculos, catalogo, estadisticas y financiamiento.
export const ROLES_GESTION: string[] = [
  Roles.SuperAdministrador,
  Roles.Administrador
];

/// Solicitudes. Incluye al Operador.
export const ROLES_ATENCION: string[] = [
  Roles.SuperAdministrador,
  Roles.Administrador,
  Roles.Operador
];
