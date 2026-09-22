import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { VehiculoPublicoService } from '../../../core/services/vehiculo-publico.service';
import { SeoService } from '../../../core/services/seo.service';
import { Contacto } from '../../../core/config/contacto';
import { VehiculoPublico } from '../../../core/models/vehiculo-publico.model';
import { FinanciamientoPublico } from '../../../core/models/financiamiento.model';
import { TarjetaVehiculoComponent } from '../comunes/tarjeta-vehiculo.component';
import { IconoComponent } from '../comunes/icono.component';

@Component({
  selector: 'app-inicio',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe, TarjetaVehiculoComponent, IconoComponent],
  templateUrl: './inicio.component.html',
  styleUrl: './inicio.component.scss'
})
export class InicioComponent {
  private servicio = inject(VehiculoPublicoService);
  readonly contacto = Contacto;

  destacado = signal<VehiculoPublico | null>(null);
  financiamiento = signal<FinanciamientoPublico | null>(null);
  recientes = signal<VehiculoPublico[]>([]);
  total = signal(0);
  cargando = signal(true);

  readonly mensajeGeneral =
    'Hola, vi el sitio de Importaciones del Caribe y quiero consultar por un vehículo.';

  readonly mensajeBusqueda =
    'Hola, busco un vehículo que no vi en el catálogo. ¿Me pueden ayudar a encontrarlo?';

  readonly mensajeFinanciamiento =
    'Hola, quiero consultar por el financiamiento de un vehículo. ¿Cómo funciona?';

  /// Los pasos del proceso. Quien nunca importó un carro no sabe qué
  /// tiene que hacer ni cuándo paga: contarlo quita la mayor barrera
  /// de esta venta.
  readonly pasos = [
    {
      titulo: 'Elegís el vehículo',
      texto: 'Del catálogo, o decinos qué buscás y lo conseguimos en Estados Unidos.'
    },
    {
      titulo: 'Confirmamos el precio final',
      texto: 'Un solo número, puesto en Costa Rica. Impuestos y trámites incluidos.'
    },
    {
      titulo: 'Lo traemos',
      texto: 'Compra, traslado y nacionalización. Te vamos contando en qué va.'
    },
    {
      titulo: 'Lo recibís con placas',
      texto: 'Inscrito y a tu nombre, listo para manejar.'
    }
  ];

  constructor() {
    inject(SeoService).setear({
      titulo: 'Importaciones del Caribe CR · Vehículos de USA a Costa Rica',
      descripcion:
        'Vehículos traídos de Estados Unidos con el precio final puesto en Costa Rica: ' +
        'impuestos, traslado y trámites incluidos. Contado o financiado.',
      ruta: '/'
    });

    // Las dos peticiones salen juntas. En el servidor esto importa:
    // la página no se entrega hasta tener las dos, y en paralelo
    // tardan lo que tarda la más lenta, no la suma.
    forkJoin({
      destacado: this.servicio.destacado(),
      recientes: this.servicio.listar({ porPagina: 7 }),
      financiamiento: this.servicio.financiamiento()
    }).subscribe({
      next: ({ destacado, recientes, financiamiento }) => {
        this.financiamiento.set(financiamiento);
        this.destacado.set(destacado);

        // El destacado ya se muestra arriba: se saca de la lista de
        // abajo para no repetirlo dos veces en la misma pantalla.
        this.recientes.set(
          recientes.items.filter(v => v.slug !== destacado?.slug).slice(0, 6));

        this.total.set(recientes.totalRegistros);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  consultarDestacado(v: VehiculoPublico): string {
    return this.contacto.whatsappUrl(
      `Hola, me interesa el ${v.marca} ${v.modelo} ${v.anio} que vi en el sitio.`);
  }
}
