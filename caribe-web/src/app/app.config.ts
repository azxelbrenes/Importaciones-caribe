import {
  ApplicationConfig,
  PLATFORM_ID,
  inject,
  provideAppInitializer,
  provideZonelessChangeDetection
} from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  provideClientHydration,
  withHttpTransferCacheOptions
} from '@angular/platform-browser';
import { isPlatformBrowser } from '@angular/common';
import { firstValueFrom, of } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { apiServidorInterceptor } from './core/http/api-servidor';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),

    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled'
      })
    ),

    provideHttpClient(
      withFetch(),
      // apiServidor va primero: completa la dirección antes de que el
      // de autenticación agregue el token. En el navegador no hace nada.
      withInterceptors([apiServidorInterceptor, authInterceptor])
    ),

    provideClientHydration(
      // Lo que el servidor ya pidió viaja dentro del HTML. El navegador
      // lo reutiliza en vez de pedirlo otra vez: el catálogo aparece
      // de una, sin parpadear mientras recarga.
      //
      // Los POST no se guardan: son acciones, y repetirlas tendría efectos.
      withHttpTransferCacheOptions({ includePostRequests: false })
    ),

    // Al arrancar, intenta recuperar la sesión con la cookie. Solo en
    // el navegador: en el servidor no hay cookie del usuario.
    provideAppInitializer(() => {
      const plataforma = inject(PLATFORM_ID);
      const auth = inject(AuthService);

      if (!isPlatformBrowser(plataforma)) {
        auth.sesionVerificada.set(true);
        return firstValueFrom(of(true));
      }

      return firstValueFrom(auth.restaurarSesion());
    })
  ]
};
