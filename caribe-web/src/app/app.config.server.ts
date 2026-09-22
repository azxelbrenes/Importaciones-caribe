import { ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { API_SERVIDOR } from './core/http/api-servidor';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),

    // En producción, el contenedor web habla con el API por la red
    // interna de Docker: http://api:8080. No sale a internet ni pasa
    // por Caddy, así que es más rápido y no depende del certificado.
    //
    // En desarrollo no está definida y usa el puerto local del API.
    {
      provide: API_SERVIDOR,
      useValue: process.env['API_INTERNA'] ?? 'http://localhost:5245'
    }
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
