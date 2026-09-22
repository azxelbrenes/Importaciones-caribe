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

  // Estas rutas no llevan token y no se reintentan: hacerlo seria un
  // bucle infinito de renovaciones fallidas.
  const esDeAuth = req.url.includes('/auth/refrescar')
                || req.url.includes('/auth/login');

  // El token con el que sale ESTA peticion. Se guarda para saber,
  // si falla, si otra peticion ya consiguio uno nuevo mientras tanto.
  const tokenUsado = auth.accessToken;

  const conToken = tokenUsado && !esDeAuth
    ? req.clone({
        setHeaders: { Authorization: `Bearer ${tokenUsado}` },
        withCredentials: true
      })
    : req.clone({ withCredentials: true });

  const reintentar = (token: string) =>
    next(req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
      withCredentials: true
    }));

  return next(conToken).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || esDeAuth) {
        return throwError(() => error);
      }

      // Si mientras esta peticion viajaba otra ya renovo el token, se
      // reintenta con ese directamente. Pedir otra renovacion rotaria
      // la cookie de nuevo sin ninguna necesidad.
      const actual = auth.accessToken;
      if (actual && actual !== tokenUsado) {
        return reintentar(actual);
      }

      // Si no, se renueva. Si ya hay una renovacion en camino, refrescar()
      // devuelve esa misma: nunca salen dos a la vez.
      return auth.refrescar().pipe(
        switchMap(nuevo => {
          if (!nuevo) {
            auth.limpiar();
            router.navigate(['/admin/login']);
            return throwError(() => error);
          }

          return reintentar(nuevo.accessToken);
        })
      );
    })
  );
};