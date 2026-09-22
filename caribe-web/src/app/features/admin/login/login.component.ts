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

    if (m === 'password' || m === 'restablecida')
      this.motivo.set('Contraseña cambiada. Entrá con la nueva.');
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
          // La contraseña NO se borra acá: el backend la vuelve a
          // pedir junto con el código, porque el login no guarda
          // estado entre los dos pasos. Borrarla hacía que el segundo
          // envío llegara vacío y fuera rechazado.
          this.paso.set(2);
          return;
        }

        // Recién ahora deja de hacer falta en memoria.
        this.password.set('');
        this.codigo.set('');

        // Vuelve a donde intentaba entrar, o al panel.
        const volver = this.ruta.snapshot.queryParamMap.get('volver');
        this.router.navigateByUrl(volver ?? '/admin');
      },
      error: (e) => {
        this.enviando.set(false);
        this.error.set(this.mensajeDeError(e));
      }
    });
  }

  volverAlPaso1(): void {
    this.paso.set(1);
    this.codigo.set('');
    this.password.set('');
    this.error.set(null);
  }

  /// Traduce la respuesta del servidor a un mensaje útil.
  ///
  /// Antes todo lo que no traía "mensaje" terminaba en "No pudimos
  /// verificar sus credenciales", y eso escondió el error del doble
  /// factor: la validación respondía, pero con otro formato.
  private mensajeDeError(e: { status?: number; error?: any }): string {
    if (e?.error?.mensaje) return e.error.mensaje;

    // FluentValidation devuelve un arreglo de errores por campo.
    if (e?.error?.errors) {
      return Object.values(e.error.errors as Record<string, string[]>)
        .flat()
        .join(' · ');
    }

    switch (e?.status) {
      case 0:
        return 'No hay conexión con el servidor. Revisá tu internet.';
      case 429:
        return 'Demasiados intentos. Esperá un minuto e intentá de nuevo.';
      case 502:
      case 503:
      case 504:
        return 'El servidor se está actualizando. Probá de nuevo en un minuto.';
      default:
        return 'No pudimos verificar sus credenciales.';
    }
  }

  /// Deja solo dígitos y corta en 6: la aplicación siempre da seis.
  alEscribirCodigo(valor: string): void {
    this.codigo.set(valor.replace(/\D/g, '').slice(0, 6));
  }
}
