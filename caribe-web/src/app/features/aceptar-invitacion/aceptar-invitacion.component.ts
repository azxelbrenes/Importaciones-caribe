import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { UsuarioService } from '../../core/services/usuario.service';

@Component({
  selector: 'app-aceptar-invitacion',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './aceptar-invitacion.component.html',
  styleUrl: './aceptar-invitacion.component.scss'
})
export class AceptarInvitacionComponent {
  private servicio = inject(UsuarioService);
  private ruta = inject(ActivatedRoute);
  private router = inject(Router);

  token = signal('');
  nombre = signal('');
  password = signal('');
  confirmar = signal('');
  verPassword = signal(false);

  enviando = signal(false);
  listo = signal(false);
  error = signal<string | null>(null);

  /// Las mismas reglas que valida el backend, mostradas mientras se
  /// escribe para corregir en el momento.
  reglas = computed(() => {
    const p = this.password();
    return {
      largo: p.length >= 10,
      letra: /[a-zA-Z]/.test(p),
      numero: /\d/.test(p),
      coinciden: p.length > 0 && p === this.confirmar()
    };
  });

  valido = computed(() => {
    const r = this.reglas();
    return this.token().length > 0
        && this.nombre().trim().length >= 3
        && r.largo && r.letra && r.numero && r.coinciden;
  });

  constructor() {
    inject(Title).setTitle('Activar cuenta · Importaciones del Caribe CR');

    // Fuera de buscadores: es una página de un solo uso.
    inject(Meta).updateTag({ name: 'robots', content: 'noindex, nofollow' });

    this.token.set(this.ruta.snapshot.queryParamMap.get('token') ?? '');
  }

  enviar(): void {
    if (!this.valido() || this.enviando()) return;

    this.enviando.set(true);
    this.error.set(null);

    this.servicio.aceptarInvitacion(
      this.token(), this.nombre().trim(), this.password(), this.confirmar()
    ).subscribe({
      next: () => {
        this.enviando.set(false);
        this.listo.set(true);
        this.password.set('');
        this.confirmar.set('');

        // Tres segundos para leer el mensaje sin quedarse esperando
        // un botón.
        setTimeout(() => this.router.navigate(['/admin/login']), 3000);
      },
      error: (e) => {
        this.enviando.set(false);
        const err = e?.error;
        this.error.set(err?.errors
          ? Object.values(err.errors as Record<string, string[]>).flat().join(' · ')
          : err?.mensaje ?? 'No pudimos activar la cuenta. El enlace puede haber vencido.');
      }
    });
  }
}
