import { Component } from '@angular/core';
import { Contacto } from '../../../core/config/contacto';
import { IconoComponent } from './icono.component';

/// El botón que acompaña toda la navegación.
///
/// En un negocio que vende por WhatsApp, el contacto no puede depender
/// de que la persona encuentre el botón correcto: tiene que estar en
/// el mismo lugar en todas las páginas.
@Component({
  selector: 'app-whatsapp-flotante',
  standalone: true,
  imports: [IconoComponent],
  template: `
    <a class="flotante" [href]="contacto.whatsappUrl(mensaje)"
       target="_blank" rel="noopener" aria-label="Escribir por WhatsApp">
      <app-icono nombre="chat" [tam]="26" />
      <span class="texto">¿Consultas? Escribinos</span>
    </a>
  `,
  styles: [`
    .flotante {
      position: fixed;
      right: 20px;
      bottom: 20px;
      z-index: 55;
      display: flex;
      align-items: center;
      gap: 10px;
      background: var(--ok);
      color: #fff;
      padding: 15px;
      border-radius: 999px;
      box-shadow: 0 14px 34px -10px rgba(31, 169, 127, .65);
      transition: transform .2s, box-shadow .2s;
    }

    .flotante:hover {
      color: #fff;
      transform: translateY(-2px);
      box-shadow: 0 18px 38px -10px rgba(31, 169, 127, .8);
    }

    .texto {
      font-family: var(--disp);
      font-size: 15px;
      font-weight: 700;
      letter-spacing: .03em;
      text-transform: uppercase;
      padding-right: 6px;
    }

    /* En el celular queda solo el icono: el texto taparía el contenido. */
    @media (max-width: 700px) {
      .flotante { right: 16px; bottom: 16px; }
      .texto { display: none; }
    }
  `]
})
export class WhatsappFlotanteComponent {
  readonly contacto = Contacto;
  readonly mensaje = 'Hola, vi el sitio de Importaciones del Caribe y quiero hacer una consulta.';
}
