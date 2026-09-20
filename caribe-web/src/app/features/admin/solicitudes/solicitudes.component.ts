import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-solicitudes',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Solicitudes"
      descripcion="Clientes que pidieron cotización desde el sitio." />

    <app-en-construccion seccion="Solicitudes" />
  `
})
export class SolicitudesComponent {}
