import { Routes } from '@angular/router';
import { authGuard, rolGuard } from './core/auth/auth.guard';
import { cambiosSinGuardarGuard } from './core/guards/cambios-sin-guardar.guard';
import { ROLES_ATENCION, ROLES_GESTION, Roles } from './core/auth/auth.model';

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
    // authGuard protege el panel completo, incluidas sus rutas hijas.
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/admin/layout/admin-layout.component')
        .then(m => m.AdminLayoutComponent),

    children: [
      {
        path: '',
        canActivate: [rolGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/resumen/resumen.component')
            .then(m => m.ResumenComponent),
        title: 'Resumen · Panel'
      },

      // ── Vehículos ──
      {
        path: 'vehiculos',
        canActivate: [rolGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/vehiculos/vehiculos.component')
            .then(m => m.VehiculosComponent),
        title: 'Vehículos · Panel'
      },
      {
        // "nuevo" va ANTES que ":id": Angular evalúa en orden y el
        // parámetro captura cualquier valor, incluido el texto "nuevo".
        path: 'vehiculos/nuevo',
        canActivate: [rolGuard],
        canDeactivate: [cambiosSinGuardarGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/vehiculos/formulario/formulario.component')
            .then(m => m.VehiculoFormularioComponent),
        title: 'Nuevo vehículo · Panel'
      },
      {
        path: 'vehiculos/:id',
        canActivate: [rolGuard],
        canDeactivate: [cambiosSinGuardarGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/vehiculos/formulario/formulario.component')
            .then(m => m.VehiculoFormularioComponent),
        title: 'Editar vehículo · Panel'
      },

      // ── Solicitudes ──
      // Atención incluye al Operador: quien atiende clientes no
      // necesita ver vehículos ni márgenes.
      {
        path: 'solicitudes',
        canActivate: [rolGuard],
        data: { roles: ROLES_ATENCION },
        loadComponent: () =>
          import('./features/admin/solicitudes/solicitudes.component')
            .then(m => m.SolicitudesComponent),
        title: 'Solicitudes · Panel'
      },

      // ── Catálogo ──
      {
        path: 'catalogo',
        canActivate: [rolGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/catalogo/catalogo.component')
            .then(m => m.CatalogoComponent),
        title: 'Marcas y modelos · Panel'
      },

      // ── Financiamiento ──
      {
        path: 'financiamiento',
        canActivate: [rolGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/admin/financiamiento/financiamiento.component')
            .then(m => m.FinanciamientoComponent),
        title: 'Financiamiento · Panel'
      },

      // ── Usuarios ──
      {
        path: 'usuarios',
        canActivate: [rolGuard],
        data: { roles: [Roles.SuperAdministrador] },
        loadComponent: () =>
          import('./features/admin/usuarios/usuarios.component')
            .then(m => m.UsuariosComponent),
        title: 'Usuarios · Panel'
      },

      // ── Mi cuenta ──
      // Sin rolGuard a propósito: cualquier rol autenticado debe poder
      // activar la verificación en dos pasos.
      {
        path: 'cuenta',
        loadComponent: () =>
          import('./features/admin/cuenta/cuenta.component')
            .then(m => m.CuentaComponent),
        title: 'Mi cuenta · Panel'
      }
    ]
  },

  // Provisional mientras se construye el sitio público.
  { path: '', redirectTo: 'admin/login', pathMatch: 'full' },

  // El comodín SIEMPRE al final.
  { path: '**', redirectTo: 'admin/login' }
];
