import { Component, HostListener, RESPONSE_INIT, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { VehiculoPublicoService } from '../../../core/services/vehiculo-publico.service';
import { SeoService } from '../../../core/services/seo.service';
import { Contacto } from '../../../core/config/contacto';
import { VehiculoDetalle } from '../../../core/models/vehiculo-detalle.model';
import { ESTADO_EN_TRATO } from '../../../core/models/vehiculo-publico.model';
import { IconoComponent } from '../comunes/icono.component';
import { InteresComponent } from './interes.component';

@Component({
  selector: 'app-ficha',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe, IconoComponent, InteresComponent],
  templateUrl: './ficha.component.html',
  styleUrl: './ficha.component.scss'
})
export class FichaComponent {
  private servicio = inject(VehiculoPublicoService);
  private seo = inject(SeoService);
  private respuesta = inject(RESPONSE_INIT, { optional: true });

  readonly contacto = Contacto;
  readonly EN_TRATO = ESTADO_EN_TRATO;

  v = signal<VehiculoDetalle | null>(null);
  cargando = signal(true);
  noExiste = signal(false);

  indiceFoto = signal(0);
  ampliada = signal(false);

  mostrarInteres = signal(false);
  plazoElegido = signal<number | null>(null);

  fotoActual = computed(() => this.v()?.fotos[this.indiceFoto()] ?? null);

  constructor() {
    inject(ActivatedRoute).paramMap.pipe(
      switchMap(p => {
        this.cargando.set(true);
        this.noExiste.set(false);
        this.indiceFoto.set(0);
        return this.servicio.detalle(p.get('slug') ?? '');
      })
    ).subscribe({
      next: (v) => {
        this.v.set(v);
        this.cargando.set(false);
        this.aplicarSeo(v);
      },
      error: () => {
        this.cargando.set(false);
        this.noExiste.set(true);

        // Un vehículo vendido deja de existir para el sitio. Devolver
        // 404 evita que Google lo siga mostrando en los resultados.
        if (this.respuesta) this.respuesta.status = 404;

        this.seo.setear({
          titulo: 'Vehículo no disponible',
          descripcion: 'Este vehículo ya no está disponible. Mirá el catálogo actual.'
        });
      }
    });
  }

  /// Lo que ve alguien cuando pegan el enlace en un chat: foto, nombre
  /// y precio. En un negocio que vende por WhatsApp, esto es la vitrina.
  private aplicarSeo(v: VehiculoDetalle): void {
    const precio = v.precioPublicado.toLocaleString('en-US', {
      style: 'currency', currency: 'USD', maximumFractionDigits: 0
    });

    this.seo.setear({
      titulo: `${v.marca} ${v.modelo} ${v.anio} · ${precio}`,
      descripcion:
        `${v.marca} ${v.modelo} ${v.anio}, ${v.kilometraje.toLocaleString('es-CR')} km, ` +
        `${v.transmision.toLowerCase()}. Precio final puesto en Costa Rica, ` +
        'con impuestos y trámites incluidos.',
      ruta: `/vehiculos/${v.slug}`,
      imagen: v.fotos.find(f => f.esPortada)?.url ?? v.fotos[0]?.url,
      tipo: 'product'
    });
  }

  // ── Galería ──

  verFoto(i: number): void { this.indiceFoto.set(i); }

  mover(paso: 1 | -1): void {
    const total = this.v()?.fotos.length ?? 0;
    if (total === 0) return;

    // Da la vuelta: de la última a la primera, y al revés.
    this.indiceFoto.update(i => (i + paso + total) % total);
  }

  @HostListener('document:keydown', ['$event'])
  alTeclear(e: KeyboardEvent): void {
    if (!this.ampliada()) return;

    if (e.key === 'Escape') this.ampliada.set(false);
    if (e.key === 'ArrowRight') this.mover(1);
    if (e.key === 'ArrowLeft') this.mover(-1);
  }

  // ── Consulta ──

  abrirInteres(plazo: number | null = null): void {
    this.plazoElegido.set(plazo);
    this.mostrarInteres.set(true);
  }

  mensajeDirecto(): string {
    const v = this.v();
    if (!v) return this.contacto.whatsappUrl();

    return this.contacto.whatsappUrl(
      `Hola, me interesa el ${v.marca} ${v.modelo} ${v.anio} que vi en el sitio.`);
  }
}
