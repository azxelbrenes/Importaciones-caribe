import { Component, HostListener, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { Contacto } from '../../../core/config/contacto';
import { IconoComponent } from './icono.component';

@Component({
  selector: 'app-encabezado',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, IconoComponent],
  template: `
    <header class="barra" [class.solida]="desplazado() || abierto()">
      <div class="envoltura">
        <a class="marca" routerLink="/" (click)="cerrar()">
          <img src="logo.jpg" alt="" width="42" height="42">
          <span>
            <strong>Importaciones del Caribe</strong>
            <small>De USA a Costa Rica</small>
          </span>
        </a>

        <nav class="enlaces" [class.abierto]="abierto()" aria-label="Principal">
          <a routerLink="/vehiculos" routerLinkActive="activo" (click)="cerrar()">Catálogo</a>
          <a routerLink="/" fragment="como-funciona" (click)="cerrar()">Cómo funciona</a>
          <a routerLink="/" fragment="financiamiento" (click)="cerrar()">Financiamiento</a>
          <a routerLink="/" fragment="nosotros" (click)="cerrar()">Nosotros</a>

          <a class="ig" [href]="contacto.instagramUrl" target="_blank" rel="noopener"
             aria-label="Instagram">
            <app-icono nombre="camara" [tam]="19" />
            <span class="solo-movil">Instagram</span>
          </a>

          <a class="cta" [href]="contacto.whatsappUrl(saludo)" target="_blank" rel="noopener">
            <app-icono nombre="chat" [tam]="18" />
            Escribinos
          </a>
        </nav>

        <button class="menu" type="button"
                [attr.aria-expanded]="abierto()"
                [attr.aria-label]="abierto() ? 'Cerrar menú' : 'Abrir menú'"
                (click)="abierto.update(v => !v)">
          <app-icono [nombre]="abierto() ? 'cerrar' : 'menu'" [tam]="24" />
        </button>
      </div>
    </header>
  `,
  styleUrl: './encabezado.component.scss'
})
export class EncabezadoComponent {
  readonly contacto = Contacto;
  readonly saludo = 'Hola, vi el sitio de Importaciones del Caribe y quiero consultar por un vehículo.';

  abierto = signal(false);
  desplazado = signal(false);

  private esNavegador = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    // Al navegar se cierra el menú del celular. Sin esto, quedaría
    // abierto tapando la página a la que la persona acaba de ir.
    inject(Router).events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.abierto.set(false));
  }

  /// La barra se vuelve sólida al bajar: arriba deja ver la foto del
  /// inicio, abajo necesita fondo para que se lea sobre el contenido.
  @HostListener('window:scroll')
  alDesplazar(): void {
    if (this.esNavegador) this.desplazado.set(window.scrollY > 24);
  }

  cerrar(): void { this.abierto.set(false); }
}
