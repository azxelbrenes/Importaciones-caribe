import { Component, input } from '@angular/core';

/// Encabezado de cada pantalla del panel. Existe para que las siete
/// secciones tengan el mismo tratamiento sin repetir CSS.
///
/// El <ng-content> permite que cada pantalla ponga sus propios
/// botones a la derecha del titulo.
@Component({
  selector: 'app-encabezado-seccion',
  standalone: true,
  template: `
    <header class="cabecera">
      <div class="texto">
        <h1>{{ titulo() }}</h1>
        @if (descripcion()) { <p>{{ descripcion() }}</p> }
      </div>
      <div class="acciones"><ng-content /></div>
    </header>
  `,
  styles: [`
    .cabecera {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 18px;
      flex-wrap: wrap;
      margin-bottom: 24px;
      padding-bottom: 18px;
      border-bottom: 1px solid var(--line);
    }
    h1 {
      font-family: var(--disp);
      font-size: 30px;
      font-weight: 800;
      text-transform: uppercase;
      line-height: 1;
      margin: 0;
    }
    p {
      font-size: 14px;
      color: var(--muted);
      margin: 8px 0 0;
      max-width: 62ch;
      line-height: 1.5;
    }
    .acciones { display: flex; gap: 10px; flex-wrap: wrap; }
  `]
})
export class EncabezadoSeccionComponent {
  titulo = input.required<string>();
  descripcion = input<string>('');
}
