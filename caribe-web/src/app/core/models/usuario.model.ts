/// Espejo de UsuarioDto.
export interface Usuario {
  id: string;
  nombreCompleto: string;
  email: string;
  roles: string[];
  activo: boolean;
  dobleFactorActivo: boolean;
  ultimoAcceso: string | null;
  creadoEn: string;
}

/// Espejo de InvitacionDto. Nunca incluye el token ni su hash.
export interface Invitacion {
  id: number;
  email: string;
  rol: number;
  rolTexto: string;
  invitadoPorId: string;
  expiraEn: string;
  usada: boolean;
  revocada: boolean;
  vigente: boolean;
  creadoEn: string;
}

/// enlaceTemporal viene solo si el correo NO salio de verdad. Cuando
/// Resend este activo llega null y el panel deja de mostrarlo solo.
export interface InvitacionCreada {
  id: number;
  email: string;
  correoEnviado: boolean;
  enlaceTemporal: string | null;
}

export interface PasswordRestablecida {
  email: string;
  passwordTemporal: string;
}

/// SuperAdministrador no esta: ese rol es del propietario y no se
/// delega por invitacion. El backend lo valida igual.
export const RolesInvitables = [
  {
    valor: 1,
    texto: 'Administrador',
    descripcion: 'Publica vehículos, gestiona el catálogo y ve las estadísticas y márgenes.'
  },
  {
    valor: 2,
    texto: 'Operador',
    descripcion: 'Solo atiende solicitudes de clientes. No ve costos ni márgenes.'
  }
];
