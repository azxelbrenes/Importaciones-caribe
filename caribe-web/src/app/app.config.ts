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
      withInterceptors([authInterceptor])
    ),

    provideClientHydration(
      // Las peticiones POST no se guardan en el caché de transferencia:
      // son acciones, no datos, y repetirlas tendría efectos.
      withHttpTransferCacheOptions({ includePostRequests: false })
    ),

    // Al arrancar, intenta recuperar la sesión con la cookie. Sin
    // esto, refrescar la página dentro del panel devolvería al login
    // aunque la sesión siguiera viva.
    //
    // Solo en el navegador: en el servidor no hay cookie del usuario
    // y el intento fallaría en cada renderizado.
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
