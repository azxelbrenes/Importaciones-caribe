import { Component } from '@angular/core';
import { ProximamenteComponent } from '../comunes/proximamente.component';

@Component({
  selector: 'app-ficha',
  standalone: true,
  imports: [ProximamenteComponent],
  template: `<app-proximamente titulo="Ficha del vehículo" />`
})
export class FichaComponent {}
