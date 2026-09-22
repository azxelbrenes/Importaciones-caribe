import { Component } from '@angular/core';
import { ProximamenteComponent } from '../comunes/proximamente.component';

@Component({
  selector: 'app-catalogo-publico',
  standalone: true,
  imports: [ProximamenteComponent],
  template: `<app-proximamente titulo="Catálogo" />`
})
export class CatalogoPublicoComponent {}
