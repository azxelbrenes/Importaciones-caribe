import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/// Agrega el token a cada peticion y renueva la sesion cuando expira.
///
/// El access token dura 15 minutos. Sin esto, alguien quedaria fuera
/// a mitad de una tarea: escribe un vehiculo durante veinte minutos,
/// guarda, y recibe 401.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Estas dos rutas no llevan token y no se reintentan: hacerlo
  // seria un bucle infinito de refrescos fallidos.
  const esDeAuth = req.url.includes('/auth/refrescar')
                || req.url.includes('/auth/login');

  const token = auth.accessToken;

  const conToken = token && !esDeAuth
    ? req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
        withCredentials: true
      })
    : req.clone({ withCredentials: true });

  return next(conToken).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || esDeAuth) {
        return throwError(() => error);
      }

      return auth.refrescar().pipe(
        switchMap(nuevo => {
          if (!nuevo) {
            auth.limpiar();
            router.navigate(['/admin/login']);
            return throwError(() => error);
          }

          // Se repite la peticion original con el token nuevo.
          const reintento = req.clone({
            setHeaders: { Authorization: `Bearer ${nuevo.accessToken}` },
            withCredentials: true
          });

          return next(reintento);
        })
      );
    })
  );
};
