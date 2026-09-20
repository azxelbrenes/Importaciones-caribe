import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-financiamiento',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Financiamiento"
      descripcion="Tasa, plazos y texto legal del plan de pagos." />

    <app-en-construccion seccion="Financiamiento" />
  `
})
export class FinanciamientoComponent {}
