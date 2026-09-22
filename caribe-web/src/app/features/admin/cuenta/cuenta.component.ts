import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { toDataURL } from 'qrcode';

import { CuentaService } from '../../../core/services/cuenta.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ConfigDobleFactor, Perfil, Sesion } from '../../../core/models/cuenta.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { ConfirmarComponent } from '../comunes/confirmar.component';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [FormsModule, EncabezadoSeccionComponent, ConfirmarComponent],
  templateUrl: './cuenta.component.html',
  styleUrl: './cuenta.component.scss'
})
export class CuentaComponent {
  private servicio = inject(CuentaService);
  auth = inject(AuthService);
  private router = inject(Router);

  perfil = signal<Perfil | null>(null);
  sesiones = signal<Sesion[]>([]);
  cargando = signal(true);

  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  // ── Doble factor ──
  config = signal<ConfigDobleFactor | null>(null);
  qr = signal<string | null>(null);
  codigo = signal('');
  procesando = signal(false);
  copiado = signal(false);

  desactivando = signal(false);
  passwordDesactivar = signal('');

  // ── Contraseña ──
  passActual = signal('');
  passNueva = signal('');
  passConfirmar = signal('');
  cambiandoPass = signal(false);

  // ── Sesiones ──
  confirmarCerrarOtras = signal(false);

  /// Las mismas reglas que valida el backend. Se muestran mientras se
  /// escribe para corregir en el momento, no después de enviar.
  reglas = computed(() => {
    const p = this.passNueva();
    return {
      largo: p.length >= 10,
      letra: /[a-zA-Z]/.test(p),
      numero: /\d/.test(p),
      distinta: p.length > 0 && p !== this.passActual(),
      coinciden: p.length > 0 && p === this.passConfirmar()
    };
  });

  passValida = computed(() => {
    const r = this.reglas();
    return this.passActual().length > 0
        && r.largo && r.letra && r.numero && r.distinta && r.coinciden;
  });

  otrasSesiones = computed(() => this.sesiones().filter(s => !s.esLaActual).length);

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);

    this.servicio.perfil().subscribe({
      next: (p) => { this.perfil.set(p); this.cargando.set(false); },
      error: () => {
        this.error.set('No pudimos cargar los datos de la cuenta.');
        this.cargando.set(false);
      }
    });

    this.cargarSesiones();
  }

  cargarSesiones(): void {
    this.servicio.sesiones().subscribe({
      next: (s) => this.sesiones.set(s),
      error: () => this.sesiones.set([])
    });
  }

  private limpiarAvisos(): void {
    this.error.set(null);
    this.mensaje.set(null);
  }

  // ══════════════ DOBLE FACTOR ══════════════

  iniciarDobleFactor(): void {
    this.procesando.set(true);
    this.limpiarAvisos();

    this.servicio.configurarDobleFactor().subscribe({
      next: async (c) => {
        this.config.set(c);

        // El QR se genera en el navegador, no en el servidor: la clave
        // secreta no necesita viajar como imagen, y así no se guarda
        // en ningún registro ni caché intermedio.
        try {
          this.qr.set(await toDataURL(c.uriAutenticador, {
            margin: 1,
            width: 220,
            color: { dark: '#05070A', light: '#FFFFFF' }
          }));
        } catch {
          // Sin QR queda la clave manual, que funciona igual.
          this.qr.set(null);
        }

        this.procesando.set(false);
      },
      error: (e) => {
        this.procesando.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo iniciar la configuración.');
      }
    });
  }

  cancelarDobleFactor(): void {
    this.config.set(null);
    this.qr.set(null);
    this.codigo.set('');
  }

  activarDobleFactor(): void {
    if (this.codigo().length !== 6 || this.procesando()) return;

    this.procesando.set(true);
    this.limpiarAvisos();

    this.servicio.activarDobleFactor(this.codigo()).subscribe({
      next: () => {
        this.procesando.set(false);
        this.cancelarDobleFactor();
        this.mensaje.set(
          'Verificación en dos pasos activada. La próxima vez que entres ' +
          'te va a pedir el código de la aplicación.');
        this.cargar();
      },
      error: (e) => {
        this.procesando.set(false);
        this.error.set(e?.error?.mensaje ?? 'El código no es correcto.');
      }
    });
  }

  desactivarDobleFactor(): void {
    if (!this.passwordDesactivar() || this.procesando()) return;

    this.procesando.set(true);
    this.limpiarAvisos();

    this.servicio.desactivarDobleFactor(this.passwordDesactivar()).subscribe({
      next: () => {
        this.procesando.set(false);
        this.desactivando.set(false);
        this.passwordDesactivar.set('');
        this.mensaje.set('Verificación en dos pasos desactivada.');
        this.cargar();
      },
      error: (e) => {
        this.procesando.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo desactivar.');
      }
    });
  }

  alEscribirCodigo(valor: string): void {
    this.codigo.set(valor.replace(/\D/g, '').slice(0, 6));
  }

  copiarClave(): void {
    const clave = this.config()?.claveManual;
    if (!clave || typeof navigator === 'undefined') return;

    navigator.clipboard.writeText(clave.replace(/\s/g, ''))
      .then(() => {
        this.copiado.set(true);
        setTimeout(() => this.copiado.set(false), 2000);
      })
      .catch(() => this.error.set('No se pudo copiar. Escribila a mano.'));
  }

  // ══════════════ CONTRASEÑA ══════════════

  cambiarPassword(): void {
    if (!this.passValida() || this.cambiandoPass()) return;

    this.cambiandoPass.set(true);
    this.limpiarAvisos();

    this.servicio.cambiarPassword(this.passActual(), this.passNueva()).subscribe({
      next: () => {
        this.cambiandoPass.set(false);

        // El backend cierra TODAS las sesiones al cambiar la
        // contraseña, incluida esta. En vez de dejar que la próxima
        // petición falle sin explicación, se sale ya y se avisa.
        this.auth.limpiar();
        this.router.navigate(['/admin/login'], {
          queryParams: { motivo: 'password' }
        });
      },
      error: (e) => {
        this.cambiandoPass.set(false);
        const err = e?.error;

        this.error.set(
          err?.errors
            ? Object.values(err.errors as Record<string, string[]>).flat().join(' · ')
            : err?.mensaje ?? 'No se pudo cambiar la contraseña.');
      }
    });
  }

  // ══════════════ SESIONES ══════════════

  cerrarOtras(): void {
    this.confirmarCerrarOtras.set(false);
    this.limpiarAvisos();

    this.servicio.cerrarOtras().subscribe({
      next: (n) => {
        this.mensaje.set(n === 0
          ? 'No había otras sesiones abiertas.'
          : `Se ${n === 1 ? 'cerró 1 sesión' : `cerraron ${n} sesiones`}.`);
        this.cargarSesiones();
      },
      error: () => this.error.set('No se pudieron cerrar las sesiones.')
    });
  }

  fecha(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('es-CR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
