import {
  Component, HostListener, PLATFORM_ID, afterNextRender, inject, signal
} from '@angular/core';
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
    <header class="barra" [class.solida]="conFondo()">
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
                (click)="alternar()">
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

  /// Solo el inicio tiene una imagen grande detrás de la barra. En las
  /// demás páginas, dejarla transparente la hace ver suspendida sobre
  /// el contenido.
  esInicio = signal(true);

  conFondo = signal(true);

  private esNavegador = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    const router = inject(Router);

    this.actualizarRuta(router.url);

    router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.abierto.set(false);
        this.actualizarRuta((e as NavigationEnd).urlAfterRedirects);

        // Al cambiar de página el navegador vuelve arriba, pero el
        // evento de desplazamiento no se dispara. Sin esto, la barra
        // quedaría con el estado de la página anterior.
        this.leerPosicion();
      });

    // Al entrar, o al volver a una pestaña que el navegador restauró
    // desplazada, tampoco hay evento. Se lee la posición una vez.
    afterNextRender(() => this.leerPosicion());
  }

  private actualizarRuta(url: string): void {
    const limpia = url.split(/[?#]/)[0];
    this.esInicio.set(limpia === '/' || limpia === '');
    this.recalcular();
  }

  private leerPosicion(): void {
    if (this.esNavegador) this.desplazado.set(window.scrollY > 24);
    this.recalcular();
  }

  /// La barra lleva fondo salvo en un caso: el inicio, sin desplazar y
  /// con el menú cerrado.
  private recalcular(): void {
    this.conFondo.set(!this.esInicio() || this.desplazado() || this.abierto());
  }

  @HostListener('window:scroll')
  alDesplazar(): void { this.leerPosicion(); }

  alternar(): void {
    this.abierto.update(v => !v);
    this.recalcular();
  }

  cerrar(): void {
    this.abierto.set(false);
    this.recalcular();
  }
}
