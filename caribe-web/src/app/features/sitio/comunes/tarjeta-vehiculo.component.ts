import { Component, input } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ESTADO_EN_TRATO, VehiculoPublico } from '../../../core/models/vehiculo-publico.model';

/// La tarjeta del catálogo. Se usa en el inicio y en el catálogo, así
/// un vehículo se ve igual en los dos lugares.
@Component({
  selector: 'app-tarjeta-vehiculo',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe],
  template: `
    <a class="tarjeta" [routerLink]="['/vehiculos', v().slug]">
      <div class="foto">
        @if (v().fotoPortada) {
          <!-- Ancho y alto declarados: el navegador reserva el espacio
               antes de que la imagen cargue, y la página no salta. -->
          <img [src]="v().fotoPortada" [alt]="v().marca + ' ' + v().modelo + ' ' + v().anio"
               width="400" height="300" [attr.loading]="prioritaria() ? 'eager' : 'lazy'">
        } @else {
          <div class="sin-foto" aria-hidden="true">{{ v().marca }}</div>
        }

        <div class="insignias">
          @if (v().estado === EN_TRATO) {
            <span class="insignia trato">En trato</span>
          }
          @if (v().aceptaFinanciamiento) {
            <span class="insignia financiable">Financiable</span>
          }
        </div>
      </div>

      <div class="datos">
        <p class="nombre">{{ v().marca }} {{ v().modelo }}</p>
        <p class="detalle">
          {{ v().anio }} · {{ v().kilometraje | number }} km
          @if (v().color) { · {{ v().color }} }
        </p>

        <div class="precio">
          <strong>{{ v().precioPublicado | currency:'USD':'symbol':'1.0-0' }}</strong>
          <small>Precio final en Costa Rica</small>
        </div>
      </div>
    </a>
  `,
  styleUrl: './tarjeta-vehiculo.component.scss'
})
export class TarjetaVehiculoComponent {
  v = input.required<VehiculoPublico>();

  /// Las primeras tarjetas cargan de inmediato: son las que se ven sin
  /// bajar. Las demás esperan a que la persona llegue a ellas.
  prioritaria = input(false);

  readonly EN_TRATO = ESTADO_EN_TRATO;
}
