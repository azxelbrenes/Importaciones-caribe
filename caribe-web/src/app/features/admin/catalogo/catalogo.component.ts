import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-catalogo',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Marcas y modelos"
      descripcion="Las opciones que aparecen al publicar un vehículo y en los filtros." />

    <app-en-construccion seccion="Marcas y modelos" />
  `
})
export class CatalogoComponent {}
