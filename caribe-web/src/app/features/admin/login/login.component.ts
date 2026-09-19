import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private ruta = inject(ActivatedRoute);
  private titulo = inject(Title);

  /// 1 = correo y contraseña · 2 = código de verificación
  paso = signal<1 | 2>(1);

  email    = signal('');
  password = signal('');
  codigo   = signal('');

  verPassword = signal(false);
  enviando = signal(false);
  error = signal<string | null>(null);

  /// Aviso cuando la sesión se cerró sola por inactividad. Sin esto,
  /// la persona volvería al login sin entender por qué.
  motivo = signal<string | null>(null);

  constructor() {
    this.titulo.setTitle('Acceso al panel · Importaciones del Caribe CR');

    const m = this.ruta.snapshot.queryParamMap.get('motivo');

    if (m === 'inactividad')
      this.motivo.set('Su sesión se cerró por inactividad.');
  }

  get valido(): boolean {
    if (this.paso() === 1)
      return this.email().includes('@') && this.password().length >= 1;

    return this.codigo().replace(/\D/g, '').length === 6;
  }

  enviar(): void {
    if (!this.valido || this.enviando()) return;

    this.enviando.set(true);
    this.error.set(null);
    this.motivo.set(null);

    const codigo = this.paso() === 2 ? this.codigo() : undefined;

    this.auth.login(this.email().trim(), this.password(), codigo).subscribe({
      next: (r) => {
        this.enviando.set(false);

        if (r.requiereDobleFactor) {
          this.paso.set(2);
          // La contraseña ya no se necesita en memoria.
          this.password.set('');
          return;
        }

        // Vuelve a donde intentaba entrar, o al panel.
        const volver = this.ruta.snapshot.queryParamMap.get('volver');
        this.router.navigateByUrl(volver ?? '/admin');
      },
      error: (e) => {
        this.enviando.set(false);

        this.error.set(
          e?.error?.mensaje ??
          (e?.status === 429
            ? 'Demasiados intentos. Espere un minuto e intente de nuevo.'
            : 'No pudimos verificar sus credenciales.')
        );
      }
    });
  }

  volverAlPaso1(): void {
    this.paso.set(1);
    this.codigo.set('');
    this.password.set('');
    this.error.set(null);
  }

  /// Deja solo dígitos y corta en 6: la aplicación siempre da seis.
  alEscribirCodigo(valor: string): void {
    this.codigo.set(valor.replace(/\D/g, '').slice(0, 6));
  }
}
