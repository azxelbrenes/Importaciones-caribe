import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-resumen',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Resumen"
      descripcion="Estado general del negocio: vehículos publicados, solicitudes pendientes y ventas del mes." />

    <app-en-construccion seccion="Resumen" />
  `
})
export class ResumenComponent {}
