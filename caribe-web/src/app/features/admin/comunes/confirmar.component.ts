import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

/// Confirmacion antes de una accion destructiva.
///
/// Para lo irreversible pide escribir una palabra: un clic de mas no
/// evita el error de "le di al boton equivocado", pero escribir
/// ELIMINAR obliga a leer que se esta por hacer.
@Component({
  selector: 'app-confirmar',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="fondo" (click)="cancelar.emit()">
      <div class="caja" (click)="$event.stopPropagation()">
        <p class="titulo">{{ titulo() }}</p>
        <p class="texto">{{ mensaje() }}</p>

        @if (palabraClave()) {
          <div class="campo">
            <label for="cf-palabra">
              Escribí <strong>{{ palabraClave() }}</strong> para confirmar
            </label>
            <input id="cf-palabra" type="text" autocomplete="off"
                   [ngModel]="escrito()" (ngModelChange)="escrito.set($event)">
          </div>
        }

        <div class="acciones">
          <button class="cancelar" type="button" (click)="cancelar.emit()">
            Cancelar
          </button>
          <button class="aceptar" type="button"
                  [disabled]="!puedeConfirmar()"
                  (click)="confirmar.emit()">
            {{ textoAceptar() }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .fondo {
      position: fixed;
      inset: 0;
      z-index: 150;
      background: rgba(5,7,10,.85);
      backdrop-filter: blur(4px);
      display: grid;
      place-items: center;
      padding: 24px;
    }
    .caja {
      background: var(--panel);
      border: 1px solid var(--line2);
      border-top: 3px solid var(--red);
      padding: 26px;
      max-width: 420px;
      width: 100%;
      box-shadow: 0 30px 70px -30px rgba(0,0,0,.95);
    }
    .titulo {
      font-family: var(--disp);
      font-size: 21px;
      font-weight: 800;
      text-transform: uppercase;
      margin: 0 0 10px;
      line-height: 1.05;
    }
    .texto { font-size: 14px; color: var(--muted); margin: 0 0 18px; line-height: 1.55; }
    .campo { display: flex; flex-direction: column; gap: 7px; margin-bottom: 18px; }
    .campo label { font-size: 12.5px; color: var(--muted); }
    .campo label strong { color: var(--red); font-family: var(--mono); }
    .acciones { display: flex; gap: 10px; justify-content: flex-end; }
    .acciones button {
      font-family: var(--disp);
      font-size: 14.5px;
      font-weight: 700;
      letter-spacing: .05em;
      text-transform: uppercase;
      padding: 11px 20px;
      border: none;
    }
    .cancelar { background: transparent; border: 1px solid var(--line2); color: #C6D0DA; }
    .cancelar:hover { color: var(--txt); }
    .aceptar { background: var(--red); color: #fff; }
    .aceptar:hover:not(:disabled) { background: var(--red-l); }
    .aceptar:disabled { opacity: .4; cursor: not-allowed; }
  `]
})
export class ConfirmarComponent {
  titulo = input.required<string>();
  mensaje = input.required<string>();
  textoAceptar = input('Confirmar');

  /// Si se indica, hay que escribirla para habilitar el boton.
  palabraClave = input<string>('');

  confirmar = output<void>();
  cancelar = output<void>();

  escrito = signal('');

  puedeConfirmar(): boolean {
    const clave = this.palabraClave();
    if (!clave) return true;
    return this.escrito().trim().toUpperCase() === clave.toUpperCase();
  }
}
