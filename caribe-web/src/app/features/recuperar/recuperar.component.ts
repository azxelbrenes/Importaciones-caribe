import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-recuperar',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './recuperar.component.html',
  // Mismo diseño que aceptar invitación: son las tres puertas de entrada
  // al panel y tienen que sentirse parte del mismo lugar. Se comparte el
  // archivo en vez de copiarlo.
  styleUrl: '../aceptar-invitacion/aceptar-invitacion.component.scss'
})
export class RecuperarComponent {
  private auth = inject(AuthService);

  email = signal('');
  enviando = signal(false);
  enviado = signal(false);
  error = signal<string | null>(null);

  valido = computed(() => /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(this.email().trim()));

  constructor() {
    inject(Title).setTitle('Recuperar contraseña · Importaciones del Caribe CR');
    inject(Meta).updateTag({ name: 'robots', content: 'noindex, nofollow' });
  }

  enviar(): void {
    if (!this.valido() || this.enviando()) return;

    this.enviando.set(true);
    this.error.set(null);

    this.auth.solicitarRecuperacion(this.email().trim().toLowerCase()).subscribe({
      next: () => {
        this.enviando.set(false);
        this.enviado.set(true);
      },
      error: (e) => {
        this.enviando.set(false);

        // El backend responde Ok exista o no la cuenta. Un error acá
        // solo puede ser el límite de intentos o un problema de red:
        // nunca revela si el correo está registrado.
        this.error.set(e?.status === 429
          ? 'Hiciste varios pedidos seguidos. Esperá unos minutos e intentá de nuevo.'
          : 'No pudimos procesar el pedido. Revisá tu conexión e intentá de nuevo.');
      }
    });
  }
}
