import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Contacto } from '../../../core/config/contacto';
import { SeoService } from '../../../core/services/seo.service';

/// Términos de uso del sitio.
///
/// BORRADOR: lo más importante acá es la naturaleza de los precios
/// —referencias con vigencia, no ofertas en firme— y que un abogado
/// lo revise antes de publicarlo.
@Component({
  selector: 'app-terminos',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './terminos.component.html',
  styleUrl: './legal.component.scss'
})
export class TerminosComponent {
  readonly contacto = Contacto;
  readonly actualizado = 'setiembre de 2026';

  constructor() {
    inject(SeoService).setear({
      titulo: 'Términos de uso',
      descripcion:
        'Condiciones de uso del sitio de Importaciones del Caribe CR: precios, ' +
        'cotizaciones y alcance de la información publicada.',
      ruta: '/terminos'
    });
  }
}
