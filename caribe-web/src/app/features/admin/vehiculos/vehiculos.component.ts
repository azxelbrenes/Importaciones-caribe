import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-vehiculos',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Vehículos"
      descripcion="Publicá, editá y gestioná el catálogo completo." />

    <app-en-construccion seccion="Vehículos" />
  `
})
export class VehiculosComponent {}
