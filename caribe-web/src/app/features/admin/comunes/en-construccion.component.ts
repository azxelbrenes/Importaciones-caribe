import { Component, input } from '@angular/core';

/// Marcador para las secciones todavia no construidas.
///
/// Existe para poder probar la navegacion completa desde ahora: es
/// mas facil detectar un problema de rutas o de permisos con el
/// esqueleto vacio que cuando ya hay siete pantallas encima.
@Component({
  selector: 'app-en-construccion',
  standalone: true,
  template: `
    <div class="marcador">
      <span class="icono" aria-hidden="true">◧</span>
      <p class="titulo">{{ seccion() }}</p>
      <p class="texto">Esta sección está en construcción.</p>
    </div>
  `,
  styles: [`
    .marcador {
      border: 1px dashed var(--line2);
      background: var(--panel);
      padding: 60px 24px;
      text-align: center;
    }
    .icono { font-size: 30px; color: var(--dim); display: block; }
    .titulo {
      font-family: var(--disp);
      font-size: 20px;
      font-weight: 700;
      text-transform: uppercase;
      margin: 14px 0 6px;
    }
    .texto { font-size: 13.5px; color: var(--muted); margin: 0; }
  `]
})
export class EnConstruccionComponent {
  seccion = input.required<string>();
}
