import { Component } from '@angular/core';
import { ProximamenteComponent } from '../comunes/proximamente.component';

@Component({
  selector: 'app-inicio',
  standalone: true,
  imports: [ProximamenteComponent],
  template: `<app-proximamente titulo="Inicio" />`
})
export class InicioComponent {}
