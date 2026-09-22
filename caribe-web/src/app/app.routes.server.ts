import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // El panel se renderiza solo en el navegador: no tiene sentido que
  // el servidor dibuje pantallas que requieren sesión.
  { path: 'admin', renderMode: RenderMode.Client },
  { path: 'admin/**', renderMode: RenderMode.Client },

  // El token de invitación viaja en la URL. Renderizarla en el
  // servidor haría que ese token quedara en los registros, y es de un
  // solo uso: quien leyera el log podría usarlo antes que la persona.
  { path: 'aceptar-invitacion', renderMode: RenderMode.Client },
  { path: 'restablecer', renderMode: RenderMode.Client },

  // El sitio público sí: es lo que Google indexa y lo que WhatsApp lee.
  { path: '**', renderMode: RenderMode.Server }
];
