export const environment = {
  produccion: true,

  /// Ruta relativa: en producción Caddy enruta /api al backend, y en
  /// desarrollo lo hace el proxy de Angular.
  apiUrl: '/api',

  // ── Contacto del negocio ──
  // Se usan en el encabezado, el pie, el botón flotante y cada ficha.
  // Cambiarlos acá los cambia en todo el sitio.

  /// Solo dígitos, con el 506 adelante. CAMBIAR por el número del cliente.
  whatsapp: '50660429559',

  /// Sin la arroba. CAMBIAR por el usuario real.
   instagram: 'importcaribecr'
};
