import { Component, input } from '@angular/core';

/// Iconos del sitio, dibujados como trazos simples.
///
/// Son glifos genéricos —un globo de conversación, una cámara— y no
/// los logotipos de las marcas: el texto al lado dice a dónde lleva.
@Component({
  selector: 'app-icono',
  standalone: true,
  template: `
    <svg [attr.width]="tam()" [attr.height]="tam()" viewBox="0 0 24 24"
         fill="none" stroke="currentColor" stroke-width="1.8"
         stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
      @switch (nombre()) {
        @case ('chat') {
          <path d="M20 12a8 8 0 0 1-11.6 7.1L4 20l1-4.2A8 8 0 1 1 20 12Z"/>
          <path d="M9 11h.01M12 11h.01M15 11h.01"/>
        }
        @case ('camara') {
          <rect x="3" y="3" width="18" height="18" rx="5"/>
          <circle cx="12" cy="12" r="4"/>
          <path d="M17.5 6.5h.01"/>
        }
        @case ('menu')    { <path d="M4 7h16M4 12h16M4 17h16"/> }
        @case ('cerrar')  { <path d="M6 6l12 12M18 6 6 18"/> }
        @case ('flecha')  { <path d="M5 12h14M13 6l6 6-6 6"/> }
      }
    </svg>
  `,
  styles: [`:host { display: inline-flex; line-height: 0; }`]
})
export class IconoComponent {
  nombre = input.required<'chat' | 'camara' | 'menu' | 'cerrar' | 'flecha'>();
  tam = input(20);
}
