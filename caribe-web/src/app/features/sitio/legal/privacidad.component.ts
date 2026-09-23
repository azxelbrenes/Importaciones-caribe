import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Contacto } from '../../../core/config/contacto';
import { SeoService } from '../../../core/services/seo.service';

/// Política de privacidad, exigida por la Ley 8968 de Protección de
/// la Persona frente al Tratamiento de sus Datos Personales.
///
/// BORRADOR: describe lo que el sistema realmente hace, pero tiene
/// que revisarlo un abogado antes de publicarse.
@Component({
  selector: 'app-privacidad',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './privacidad.component.html',
  styleUrl: './legal.component.scss'
})
export class PrivacidadComponent {
  readonly contacto = Contacto;
  readonly actualizado = 'setiembre de 2026';

  constructor() {
    inject(SeoService).setear({
      titulo: 'Política de privacidad',
      descripcion:
        'Cómo Importaciones del Caribe CR recolecta, usa y protege los datos ' +
        'personales de quienes consultan por un vehículo.',
      ruta: '/privacidad'
    });
  }
}
