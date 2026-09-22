import { HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';

/// Dirección del API vista desde el servidor de Angular.
///
/// En el navegador, "/api" se completa solo con el dominio de la
/// página. En el servidor no hay página: una petición a "/api" no sabe
/// a dónde ir y falla. Este token le dice dónde está el backend.
///
/// Solo se provee en app.config.server.ts. En el navegador no existe,
/// y el interceptor no hace nada.
export const API_SERVIDOR = new InjectionToken<string>('API_SERVIDOR');

export const apiServidorInterceptor: HttpInterceptorFn = (req, next) => {
  const base = inject(API_SERVIDOR, { optional: true });

  if (!base || !req.url.startsWith('/api')) return next(req);

  return next(req.clone({ url: `${base.replace(/\/$/, '')}${req.url}` }));
};
