import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { InactividadService } from '../../../core/auth/inactividad.service';
import { AvisoInactividadComponent } from '../../../core/auth/aviso-inactividad.component';
import { ROLES_ATENCION, ROLES_GESTION, Roles } from '../../../core/auth/auth.model';

interface Seccion {
  ruta: string;
  titulo: string;
  icono: string;
  roles: string[];
  exacta?: boolean;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AvisoInactividadComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  private inactividad = inject(InactividadService);

  menuAbierto = signal(false);

  /// Cada sección declara qué roles la ven. El menú se filtra solo,
  /// así una operadora no ve opciones que igual le darían 403.
  ///
  /// Esto es comodidad de interfaz, no seguridad: quien manipule el
  /// navegador choca contra el guard, y si lo salta, contra el
  /// [Authorize] del servidor.
  private readonly todas: Seccion[] = [
    { ruta: '/admin',              titulo: 'Resumen',           icono: '▤', roles: ROLES_GESTION, exacta: true },
    { ruta: '/admin/vehiculos',    titulo: 'Vehículos',         icono: '▦', roles: ROLES_GESTION },
    { ruta: '/admin/solicitudes',  titulo: 'Solicitudes',       icono: '✉', roles: ROLES_ATENCION },
    { ruta: '/admin/catalogo',     titulo: 'Marcas y modelos',  icono: '◈', roles: ROLES_GESTION },
    { ruta: '/admin/financiamiento', titulo: 'Financiamiento',  icono: '◰', roles: ROLES_GESTION },
    { ruta: '/admin/usuarios',     titulo: 'Usuarios',          icono: '◉', roles: [Roles.SuperAdministrador] },
    { ruta: '/admin/cuenta',       titulo: 'Mi cuenta',         icono: '⚙', roles: ROLES_ATENCION }
  ];

  secciones = computed(() =>
    this.todas.filter(s => this.auth.tieneAlgunRol(s.roles)));

  /// Iniciales para el avatar. Con dos basta: más se vuelve ilegible
  /// en un círculo de 36 píxeles.
  iniciales = computed(() => {
    const nombre = this.auth.usuario()?.nombreCompleto ?? '';

    return nombre
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(p => p[0])
      .join('')
      .toUpperCase();
  });

  constructor() {
    // El contador de inactividad solo corre con sesión activa: no
    // tiene sentido vigilar a un visitante del catálogo.
    effect(() => {
      if (this.auth.autenticado()) {
        this.inactividad.iniciar();
      } else {
        this.inactividad.detener();
      }
    });
  }

  alternarMenu(): void { this.menuAbierto.update(v => !v); }
  cerrarMenu(): void { this.menuAbierto.set(false); }

  salir(): void {
    this.auth.cerrarSesion().subscribe(() => {
      this.router.navigate(['/admin/login']);
    });
  }
}
