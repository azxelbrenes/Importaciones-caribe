import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Mi cuenta"
      descripcion="Verificación en dos pasos y sesiones abiertas." />

    <app-en-construccion seccion="Mi cuenta" />
  `
})
export class CuentaComponent {}
