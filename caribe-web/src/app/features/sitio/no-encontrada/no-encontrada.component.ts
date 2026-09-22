import { Component, RESPONSE_INIT, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Contacto } from '../../../core/config/contacto';
import { SeoService } from '../../../core/services/seo.service';

@Component({
  selector: 'app-no-encontrada',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="pagina">
      <p class="codigo">404</p>
      <h1>Esta página no existe</h1>
      <p class="texto">
        Puede que el vehículo ya se haya vendido, o que el enlace esté
        incompleto. Mirá lo que tenemos disponible ahora.
      </p>
      <div class="acciones">
        <a class="principal" routerLink="/vehiculos">Ver el catálogo</a>
        <a class="secundario" [href]="contacto.whatsappUrl(mensaje)" target="_blank" rel="noopener">
          Preguntar por WhatsApp
        </a>
      </div>
    </section>
  `,
  styles: [`
    .pagina {
      min-height: 70vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 130px 22px 80px;
    }

    .codigo {
      font-family: var(--disp);
      font-size: clamp(90px, 18vw, 170px);
      font-weight: 800;
      line-height: .9;
      background: linear-gradient(90deg, var(--blue), #fff 50%, var(--red));
      -webkit-background-clip: text;
      background-clip: text;
      color: transparent;
    }

    h1 { font-size: clamp(28px, 5vw, 42px); text-transform: uppercase; margin-top: 12px; }
    .texto { color: var(--muted); max-width: 46ch; margin-top: 14px; }

    .acciones { display: flex; gap: 12px; flex-wrap: wrap; justify-content: center; margin-top: 28px; }

    .acciones a {
      font-family: var(--disp);
      font-size: 16px;
      font-weight: 700;
      letter-spacing: .05em;
      text-transform: uppercase;
      padding: 14px 24px;
    }

    .principal { background: var(--red); color: #fff; }
    .principal:hover { background: var(--red-l); color: #fff; }
    .secundario { border: 1px solid var(--line2); color: var(--txt); }
    .secundario:hover { border-color: var(--ok); color: var(--ok); }
  `]
})
export class NoEncontradaComponent {
  readonly contacto = Contacto;
  readonly mensaje = 'Hola, llegué a una página que no existe en el sitio. ¿Me ayudan a encontrar un vehículo?';

  constructor() {
    // Responde 404 de verdad al renderizar en el servidor. Sin esto,
    // Google recibe un 200 y guarda la página "no existe" como si
    // fuera contenido real del sitio.
    const respuesta = inject(RESPONSE_INIT, { optional: true });
    if (respuesta) respuesta.status = 404;

    inject(SeoService).setear({
      titulo: 'Página no encontrada',
      descripcion: 'La página que buscás no existe. Mirá los vehículos disponibles.'
    });
  }
}
