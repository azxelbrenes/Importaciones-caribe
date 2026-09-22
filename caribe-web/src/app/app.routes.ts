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

  // Sin guard: quien la abre olvidó la contraseña, así que no tiene
  // sesión. Va ANTES de 'admin' para que el authGuard no la intercepte.
  {
    path: 'admin/recuperar',
    loadComponent: () =>
      import('./features/recuperar/recuperar.component')
        .then(m => m.RecuperarComponent),
    title: 'Recuperar contraseña · Importaciones del Caribe CR'
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

  // ══════════════════ PÚBLICAS DEL PANEL ══════════════════

  // Sin guard: quien la abre todavía NO tiene cuenta. Exigir sesión
  // haría imposible aceptar una invitación.
  {
    path: 'aceptar-invitacion',
    loadComponent: () =>
      import('./features/aceptar-invitacion/aceptar-invitacion.component')
        .then(m => m.AceptarInvitacionComponent),
    title: 'Activar cuenta · Importaciones del Caribe CR'
  },

  // El correo de recuperación apunta acá: /restablecer?token=…
  // La ruta la define el backend en UsuarioLN, no se puede cambiar
  // sin cambiar también el enlace del correo.
  {
    path: 'restablecer',
    loadComponent: () =>
      import('./features/restablecer/restablecer.component')
        .then(m => m.RestablecerComponent),
    title: 'Nueva contraseña · Importaciones del Caribe CR'
  },

  // ══════════════════ SITIO PÚBLICO ══════════════════

  // Va al FINAL: su ruta vacía captura todo lo que no coincidió antes.
  // Si estuviera arriba, se tragaría /admin y el panel sería
  // inalcanzable.
  {
    path: '',
    loadComponent: () =>
      import('./features/sitio/layout/sitio-layout.component')
        .then(m => m.SitioLayoutComponent),

    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/sitio/inicio/inicio.component')
            .then(m => m.InicioComponent)
      },
      {
        path: 'vehiculos',
        loadComponent: () =>
          import('./features/sitio/catalogo/catalogo.component')
            .then(m => m.CatalogoPublicoComponent)
      },
      {
        // Mismo patrón que usa el panel para enlazar desde una
        // solicitud: /vehiculos/toyota-tacoma-2023-blanco
        path: 'vehiculos/:slug',
        loadComponent: () =>
          import('./features/sitio/ficha/ficha.component')
            .then(m => m.FichaComponent)
      },

      // El comodín dentro del layout: la página 404 lleva encabezado y
      // pie, así quien llega a un enlace roto puede seguir navegando.
      {
        path: '**',
        loadComponent: () =>
          import('./features/sitio/no-encontrada/no-encontrada.component')
            .then(m => m.NoEncontradaComponent),
        title: 'Página no encontrada · Importaciones del Caribe CR'
      }
    ]
  }
];
