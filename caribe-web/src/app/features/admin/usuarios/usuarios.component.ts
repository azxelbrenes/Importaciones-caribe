import { Component } from '@angular/core';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { EnConstruccionComponent } from '../comunes/en-construccion.component';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [EncabezadoSeccionComponent, EnConstruccionComponent],
  template: `
    <app-encabezado-seccion
      titulo="Usuarios"
      descripcion="Invitá administradores y operadores, y gestioná sus accesos." />

    <app-en-construccion seccion="Usuarios" />
  `
})
export class UsuariosComponent {}
