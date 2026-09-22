import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-restablecer',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './restablecer.component.html',
  styleUrl: '../aceptar-invitacion/aceptar-invitacion.component.scss'
})
export class RestablecerComponent {
  private auth = inject(AuthService);
  private ruta = inject(ActivatedRoute);
  private router = inject(Router);

  token = signal('');
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
    return this.token().length > 0 && r.largo && r.letra && r.numero && r.coinciden;
  });

  constructor() {
    inject(Title).setTitle('Nueva contraseña · Importaciones del Caribe CR');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex, nofollow' });

    this.token.set(this.ruta.snapshot.queryParamMap.get('token') ?? '');
  }

  enviar(): void {
    if (!this.valido() || this.enviando()) return;

    this.enviando.set(true);
    this.error.set(null);

    this.auth.restablecer(this.token(), this.password(), this.confirmar()).subscribe({
      next: () => {
        this.enviando.set(false);
        this.listo.set(true);
        this.password.set('');
        this.confirmar.set('');

        setTimeout(() => this.router.navigate(['/admin/login'], {
          queryParams: { motivo: 'restablecida' }
        }), 3000);
      },
      error: (e) => {
        this.enviando.set(false);
        const err = e?.error;

        this.error.set(err?.errors
          ? Object.values(err.errors as Record<string, string[]>).flat().join(' · ')
          : err?.mensaje ?? 'No pudimos cambiar la contraseña. El enlace puede haber vencido.');
      }
    });
  }
}
