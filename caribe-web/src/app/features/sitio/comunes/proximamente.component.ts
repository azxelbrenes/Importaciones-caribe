import { Component, input } from '@angular/core';

/// Marcador provisional para las páginas de las próximas entregas.
/// Permite probar la navegación completa desde ahora.
@Component({
  selector: 'app-proximamente',
  standalone: true,
  template: `
    <section class="marcador">
      <p class="etq">En construcción</p>
      <h1>{{ titulo() }}</h1>
    </section>
  `,
  styles: [`
    .marcador { min-height: 70vh; display: grid; place-content: center; text-align: center; padding: 120px 22px 60px; }
    .etq { font-family: var(--mono); font-size: 11px; letter-spacing: .2em; text-transform: uppercase; color: var(--blue); }
    h1 { font-size: clamp(32px, 6vw, 56px); text-transform: uppercase; margin-top: 10px; }
  `]
})
export class ProximamenteComponent {
  titulo = input.required<string>();
}
