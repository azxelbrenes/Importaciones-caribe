import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { EncabezadoComponent } from '../comunes/encabezado.component';
import { PieComponent } from '../comunes/pie.component';
import { WhatsappFlotanteComponent } from '../comunes/whatsapp-flotante.component';

/// Marco del sitio público. El panel tiene el suyo aparte: son dos
/// productos distintos que comparten solo los colores.
@Component({
  selector: 'app-sitio-layout',
  standalone: true,
  imports: [RouterOutlet, EncabezadoComponent, PieComponent, WhatsappFlotanteComponent],
  template: `
    <a class="saltar" href="#contenido">Saltar al contenido</a>
    <app-encabezado />
    <main id="contenido">
      <router-outlet />
    </main>
    <app-pie />
    <app-whatsapp-flotante />
  `,
  styles: [`
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    main { flex: 1; }

    /* Invisible hasta que se llega con Tab. Permite a quien navega con
       teclado saltarse el menú en cada página. */
    .saltar {
      position: absolute;
      left: 12px;
      top: -60px;
      z-index: 100;
      background: var(--blue);
      color: #fff;
      padding: 10px 16px;
    }
    .saltar:focus { top: 12px; color: #fff; }
  `]
})
export class SitioLayoutComponent {}
