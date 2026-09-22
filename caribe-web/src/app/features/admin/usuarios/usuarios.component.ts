import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { UsuarioService } from '../../../core/services/usuario.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  Invitacion, InvitacionCreada, PasswordRestablecida, RolesInvitables, Usuario
} from '../../../core/models/usuario.model';

import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { ConfirmarComponent } from '../comunes/confirmar.component';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [FormsModule, EncabezadoSeccionComponent, ConfirmarComponent],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent {
  private servicio = inject(UsuarioService);
  auth = inject(AuthService);

  readonly roles = RolesInvitables;

  usuarios = signal<Usuario[]>([]);
  invitaciones = signal<Invitacion[]>([]);
  cargando = signal(true);

  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  // ── Invitar ──
  email = signal('');

  /// Operador por defecto: el rol más limitado. Si alguien se
  /// distrae al invitar, el error es dar de menos y no de más.
  rol = signal(2);

  invitando = signal(false);
  reciente = signal<InvitacionCreada | null>(null);
  copiado = signal<'enlace' | 'clave' | null>(null);

  // ── Confirmaciones ──
  aDesactivar = signal<Usuario | null>(null);
  aRevocar = signal<Invitacion | null>(null);
  aRestablecer = signal<Usuario | null>(null);
  claveNueva = signal<PasswordRestablecida | null>(null);

  vigentes = computed(() => this.invitaciones().filter(i => i.vigente));

  emailValido = computed(() =>
    /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(this.email().trim()));

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);

    this.servicio.listar().subscribe({
      next: (u) => { this.usuarios.set(u); this.cargando.set(false); },
      error: () => {
        this.error.set('No pudimos cargar los usuarios.');
        this.cargando.set(false);
      }
    });

    this.servicio.listarInvitaciones().subscribe({
      next: (i) => this.invitaciones.set(i),
      error: () => this.invitaciones.set([])
    });
  }

  private limpiarAvisos(): void {
    this.error.set(null);
    this.mensaje.set(null);
  }

  // ══════════════ INVITAR ══════════════

  invitar(): void {
    if (!this.emailValido() || this.invitando()) return;

    this.invitando.set(true);
    this.limpiarAvisos();
    this.reciente.set(null);

    const email = this.email().trim().toLowerCase();

    this.servicio.invitar(email, this.rol()).subscribe({
      next: (r) => {
        this.invitando.set(false);
        this.email.set('');

        if (r.correoEnviado) {
          this.mensaje.set(`Invitación enviada a ${email}.`);
        } else {
          // Sin correo activo, el enlace aparece acá para mandarlo a
          // mano. Cuando Resend funcione, esto deja de verse solo.
          this.reciente.set(r);
        }

        this.cargar();
      },
      error: (e) => {
        this.invitando.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo crear la invitación.');
      }
    });
  }

  copiar(texto: string | null | undefined, tipo: 'enlace' | 'clave'): void {
    if (!texto || typeof navigator === 'undefined') return;

    navigator.clipboard.writeText(texto)
      .then(() => {
        this.copiado.set(tipo);
        // Vuelve a "Copiar" a los dos segundos: si quedara en
        // "Copiado" no se sabría si el segundo intento funcionó.
        setTimeout(() => this.copiado.set(null), 2000);
      })
      .catch(() => this.error.set('No se pudo copiar. Seleccioná el texto a mano.'));
  }

  /// WhatsApp con el mensaje ya armado, sin número: la persona elige
  /// el contacto. Así no hay que redactar la explicación cada vez.
  whatsappInvitacion(): string {
    const r = this.reciente();
    if (!r?.enlaceTemporal) return 'https://wa.me/';

    const texto =
      `Te doy acceso al panel de Importaciones del Caribe CR.\n\n` +
      `Abrí este enlace para crear tu contraseña:\n${r.enlaceTemporal}\n\n` +
      `El enlace vence en 72 horas y solo sirve una vez.`;

    return `https://wa.me/?text=${encodeURIComponent(texto)}`;
  }

  // ══════════════ CUENTAS ══════════════

  confirmarDesactivar(): void {
    const u = this.aDesactivar();
    if (!u) return;

    this.servicio.activarDesactivar(u.id, !u.activo).subscribe({
      next: () => {
        this.aDesactivar.set(null);
        this.mensaje.set(u.activo
          ? `${u.nombreCompleto} ya no puede entrar. Sus sesiones se cerraron.`
          : `${u.nombreCompleto} puede volver a entrar.`);
        this.cargar();
      },
      error: (e) => {
        this.aDesactivar.set(null);
        this.error.set(e?.error?.mensaje ?? 'No se pudo cambiar el estado.');
      }
    });
  }

  confirmarRestablecer(): void {
    const u = this.aRestablecer();
    if (!u) return;

    this.servicio.restablecerPassword(u.id).subscribe({
      next: (r) => {
        this.aRestablecer.set(null);
        this.claveNueva.set(r);
      },
      error: (e) => {
        this.aRestablecer.set(null);
        this.error.set(e?.error?.mensaje ?? 'No se pudo restablecer la contraseña.');
      }
    });
  }

  confirmarRevocar(): void {
    const i = this.aRevocar();
    if (!i) return;

    this.servicio.revocarInvitacion(i.id).subscribe({
      next: () => {
        this.aRevocar.set(null);
        this.mensaje.set(`Invitación de ${i.email} revocada.`);
        this.cargar();
      },
      error: (e) => {
        this.aRevocar.set(null);
        this.error.set(e?.error?.mensaje ?? 'No se pudo revocar.');
      }
    });
  }

  // ══════════════ AUXILIARES ══════════════

  /// Nadie puede desactivarse a sí mismo: quedaría fuera de su propio
  /// panel sin forma de volver. El backend lo bloquea igual.
  esUnoMismo(u: Usuario): boolean {
    return u.email === this.auth.usuario()?.email;
  }

  esPropietario(u: Usuario): boolean {
    return u.roles.includes('SuperAdministrador');
  }

  rolDe(u: Usuario): string {
    if (u.roles.includes('SuperAdministrador')) return 'Propietario';
    if (u.roles.includes('Administrador')) return 'Administrador';
    if (u.roles.includes('Operador')) return 'Operador';
    return '—';
  }

  fecha(iso: string | null): string {
    if (!iso) return 'Nunca';
    return new Date(iso).toLocaleDateString('es-CR', {
      day: '2-digit', month: '2-digit', year: 'numeric'
    });
  }

  vence(iso: string): string {
    const horas = Math.floor((new Date(iso).getTime() - Date.now()) / 3_600_000);
    if (horas < 1) return 'vence en menos de 1 h';
    if (horas < 24) return `vence en ${horas} h`;
    return `vence en ${Math.floor(horas / 24)} días`;
  }
}
