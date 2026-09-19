export const environment = {
  produccion: true,

  /// Ruta relativa: en produccion, Caddy enruta /api al contenedor
  /// del backend. Poner el dominio completo obligaria a reconstruir
  /// el frontend si el dominio cambiara.
  apiUrl: '/api',

  whatsapp: '50683323227',
  instagram: 'importacionescaribecr'
};
