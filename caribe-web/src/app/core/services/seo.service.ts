import { Injectable, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';

interface DatosSeo {
  titulo: string;
  descripcion: string;
  /// Ruta relativa: '/vehiculos/toyota-tacoma-2023'
  ruta?: string;
  /// URL absoluta de la imagen para WhatsApp y redes.
  imagen?: string;
  tipo?: 'website' | 'product';
}

/// Título, descripción y etiquetas para compartir, por página.
///
/// Las etiquetas og: son las que lee WhatsApp cuando alguien pega un
/// enlace: sin ellas, el enlace a un vehículo llega como texto pelado.
/// Con ellas llega con la foto, el nombre y el precio. En un negocio
/// que vende por WhatsApp, eso es la vitrina.
@Injectable({ providedIn: 'root' })
export class SeoService {
  private titulo = inject(Title);
  private meta = inject(Meta);
  private doc = inject(DOCUMENT);

  private readonly SITIO = 'https://importacionescaribecr.com';
  private readonly MARCA = 'Importaciones del Caribe CR';
  private readonly IMAGEN = `${this.SITIO}/logo.jpg`;

  setear(d: DatosSeo): void {
    const titulo = d.titulo.includes(this.MARCA) ? d.titulo : `${d.titulo} · ${this.MARCA}`;
    const url = `${this.SITIO}${d.ruta ?? ''}`;
    const imagen = d.imagen ?? this.IMAGEN;

    this.titulo.setTitle(titulo);

    this.meta.updateTag({ name: 'description', content: d.descripcion });

    this.meta.updateTag({ property: 'og:title', content: titulo });
    this.meta.updateTag({ property: 'og:description', content: d.descripcion });
    this.meta.updateTag({ property: 'og:type', content: d.tipo ?? 'website' });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:image', content: imagen });
    this.meta.updateTag({ property: 'og:site_name', content: this.MARCA });
    this.meta.updateTag({ property: 'og:locale', content: 'es_CR' });

    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });

    this.canonica(url);
  }

  /// La URL "oficial" de la página. Evita que Google cuente como
  /// páginas distintas la misma ficha con y sin parámetros.
  private canonica(url: string): void {
    let link = this.doc.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');

    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(link);
    }

    link.setAttribute('href', url);
  }
}
