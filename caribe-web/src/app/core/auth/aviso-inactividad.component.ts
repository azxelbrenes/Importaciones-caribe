import { Component, inject } from '@angular/core';
import { InactividadService } from './inactividad.service';

@Component({
  selector: 'app-aviso-inactividad',
  standalone: true,
  template: `
    @if (inactividad.porExpirar()) {
      <div class="aviso" role="alert">
        <div class="caja">
          <p class="titulo">Su sesión está por cerrarse</p>
          <p class="texto">
            Por seguridad, cerramos la sesión tras 30 minutos sin
            actividad. Quedan <strong>{{ formatear() }}</strong>.
          </p>
          <button type="button" (click)="inactividad.reiniciar()">
            Seguir trabajando
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .aviso {
      position: fixed;
      inset: 0;
      z-index: 200;
      background: rgba(5, 7, 10, .85);
      backdrop-filter: blur(4px);
      display: grid;
      place-items: center;
      padding: 24px;
    }
    .caja {
      background: var(--panel);
      border: 1px solid var(--line2);
      border-top: 3px solid var(--red);
      padding: 28px;
      max-width: 400px;
      text-align: center;
      box-shadow: 0 30px 70px -30px rgba(0,0,0,.95);
    }
    .titulo {
      font-family: var(--disp);
      font-size: 21px;
      font-weight: 800;
      text-transform: uppercase;
      margin: 0 0 10px;
    }
    .texto {
      font-size: 14px;
      color: var(--muted);
      margin: 0 0 20px;
      line-height: 1.5;
    }
    .texto strong { color: var(--red); font-family: var(--mono); }
    button {
      background: var(--blue);
      color: #fff;
      border: none;
      font-family: var(--disp);
      font-size: 16px;
      font-weight: 700;
      letter-spacing: .05em;
      text-transform: uppercase;
      padding: 13px 26px;
    }
    button:hover { filter: brightness(1.15); }
  `]
})
export class AvisoInactividadComponent {
  inactividad = inject(InactividadService);

  formatear(): string {
    const s = this.inactividad.segundosRestantes();
    const m = Math.floor(s / 60);
    const r = s % 60;
    return `${m}:${r.toString().padStart(2, '0')}`;
  }
}
