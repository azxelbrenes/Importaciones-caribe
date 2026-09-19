import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [

  // ══════════════════ PANEL ══════════════════

  // El login NO lleva guard: exigir sesión para poder iniciar sesión
  // crearía un bucle del que no se sale.
  {
    path: 'admin/login',
    loadComponent: () =>
      import('./features/admin/login/login.component')
        .then(m => m.LoginComponent),
    title: 'Acceso al panel · Importaciones del Caribe CR'
  },

  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/admin/login/login.component')
        .then(m => m.LoginComponent),
    title: 'Panel · Importaciones del Caribe CR'
  },

  // Provisional mientras se construye el sitio público.
  {
    path: '',
    redirectTo: 'admin/login',
    pathMatch: 'full'
  },

  // El comodín SIEMPRE al final: captura todo lo que no coincidió
  // antes, así que cualquier ruta escrita después nunca se alcanza.
  {
    path: '**',
    redirectTo: 'admin/login'
  }
];
