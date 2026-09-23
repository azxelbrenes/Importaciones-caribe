import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Contacto } from '../../../core/config/contacto';
import { IconoComponent } from './icono.component';

@Component({
  selector: 'app-pie',
  standalone: true,
  imports: [RouterLink, IconoComponent],
  template: `
    <footer class="pie">
      <div class="franja" aria-hidden="true"></div>

      <div class="envoltura">
        <div class="columnas">
          <div class="marca">
            <img src="logo.jpg" alt="" width="56" height="56" loading="lazy">
            <p class="nombre">Importaciones del Caribe CR</p>
            <p class="lema">
              Vehículos traídos de Estados Unidos, con el precio final
              puesto en Costa Rica: impuestos, traslado y trámites incluidos.
            </p>
          </div>

          <nav aria-label="Sitio">
            <p class="titulo">Sitio</p>
            <a routerLink="/vehiculos">Catálogo</a>
            <a routerLink="/" fragment="como-funciona">Cómo funciona</a>
            <a routerLink="/" fragment="financiamiento">Financiamiento</a>
            <a routerLink="/" fragment="nosotros">Nosotros</a>
          </nav>

          <div class="contacto">
            <p class="titulo">Contacto</p>
            <a [href]="contacto.whatsappUrl()" target="_blank" rel="noopener">
              <app-icono nombre="chat" [tam]="17" />
              {{ contacto.whatsappVisible }}
            </a>
            <a [href]="contacto.instagramUrl" target="_blank" rel="noopener">
              <app-icono nombre="camara" [tam]="17" />
              {{ '@' + contacto.instagram }}
            </a>
          </div>
        </div>

        <div class="legal">
          <!-- El aviso de precios no es relleno: el avalúo de Hacienda y el
               tipo de cambio cambian, y un precio publicado es una oferta. -->
          <p>
            Los precios son de referencia y tienen vigencia limitada: pueden
            variar con el tipo de cambio y el avalúo de Hacienda.
          </p>
          <p class="fila">
            <span>© {{ anio }} Importaciones del Caribe CR</span>
            <span class="enlaces-legales">
              <a routerLink="/privacidad">Privacidad</a>
              <a routerLink="/terminos">Términos</a>
              <a routerLink="/admin/login" class="acceso">Acceso</a>
            </span>
          </p>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .pie { position: relative; background: var(--panel); border-top: 1px solid var(--line); }

    .franja {
      height: 3px;
      background: linear-gradient(90deg, var(--blue), #fff 50%, var(--red));
    }

    .envoltura { max-width: var(--wrap); margin: 0 auto; padding: 52px 22px 28px; }

    .columnas {
      display: grid;
      grid-template-columns: 1.6fr 1fr 1fr;
      gap: 40px;
      padding-bottom: 34px;
      border-bottom: 1px solid var(--line);
    }

    @media (max-width: 760px) { .columnas { grid-template-columns: 1fr; gap: 30px; } }

    .marca img { width: 56px; height: 56px; object-fit: contain; margin-bottom: 14px; }
    .nombre { font-family: var(--disp); font-size: 20px; font-weight: 800; text-transform: uppercase; }
    .lema { font-size: 14px; color: var(--muted); margin-top: 10px; max-width: 42ch; }

    .titulo {
      font-family: var(--mono);
      font-size: 10px;
      letter-spacing: .18em;
      text-transform: uppercase;
      color: var(--dim);
      margin-bottom: 14px;
    }

    nav, .contacto { display: flex; flex-direction: column; gap: 11px; }

    nav a, .contacto a {
      display: inline-flex;
      align-items: center;
      gap: 9px;
      font-size: 14.5px;
      color: #C4CFDA;
      width: fit-content;
    }

    nav a:hover, .contacto a:hover { color: #fff; }

    .legal { padding-top: 22px; font-size: 12px; color: var(--dim); line-height: 1.6; }

    .fila {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      margin-top: 12px;
    }

    .enlaces-legales { display: flex; gap: 18px; flex-wrap: wrap; }
    .enlaces-legales a { color: var(--dim); font-family: var(--mono); font-size: 11px; }
    .enlaces-legales a:hover { color: var(--blue); }
  `]
})
export class PieComponent {
  readonly contacto = Contacto;
  readonly anio = new Date().getFullYear();
}
