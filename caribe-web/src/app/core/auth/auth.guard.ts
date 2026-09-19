import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';
import { AuthService } from './auth.service';
import { Roles } from './auth.model';

/// Deja pasar solo si hay sesion. Si el token todavia no se restauro
/// —al recargar la pagina— espera a que termine el intento.
///
/// Esto es comodidad de interfaz, no seguridad: aunque alguien salte
/// el guard manipulando el navegador, el servidor devuelve 403 igual.
export const authGuard: CanActivateFn = (_ruta, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.autenticado()) return of(true);

  if (!auth.sesionVerificada()) {
    return auth.restaurarSesion().pipe(
      map(ok => ok ? true : router.createUrlTree(
        ['/admin/login'], { queryParams: { volver: estado.url } }))
    );
  }

  return of(router.createUrlTree(
    ['/admin/login'], { queryParams: { volver: estado.url } }));
};

/// Restringe por rol. Se usa con data: { roles: [...] } en la ruta.
export const rolGuard: CanActivateFn = (ruta) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const permitidos = (ruta.data?.['roles'] as string[]) ?? [];

  if (permitidos.length === 0 || auth.tieneAlgunRol(permitidos)) return true;

  return router.createUrlTree([primeraSeccionPermitida(auth)]);
};

/// La primera seccion que la persona SI puede ver.
///
/// Redirigir siempre a /admin causaria un bucle con el rol Operador:
/// esa ruta es el Resumen, que exige rol de Gestion, asi que el guard
/// lo rechazaria y lo devolveria a la misma ruta que acaba de
/// rechazarlo.
function primeraSeccionPermitida(auth: AuthService): string {
  if (auth.tieneAlgunRol([Roles.SuperAdministrador, Roles.Administrador]))
    return '/admin';

  if (auth.tieneAlgunRol([Roles.Operador]))
    return '/admin/solicitudes';

  // Sin ningun rol conocido, Mi cuenta es lo unico seguro: no exige
  // permisos y permite al menos activar la verificacion en dos pasos.
  return '/admin/cuenta';
}
