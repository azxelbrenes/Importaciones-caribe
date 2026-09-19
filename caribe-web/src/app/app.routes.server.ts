import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // El panel se renderiza solo en el navegador: no tiene sentido que
  // el servidor dibuje pantallas que requieren sesión, y evita que el
  // HTML del panel llegue a alguien sin permiso.
  { path: 'admin', renderMode: RenderMode.Client },
  { path: 'admin/**', renderMode: RenderMode.Client },

  // El sitio público sí: es lo que Google indexa y lo que WhatsApp lee.
  { path: '**', renderMode: RenderMode.Server }
];
