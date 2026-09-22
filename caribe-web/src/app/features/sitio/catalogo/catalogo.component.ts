import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap } from 'rxjs';

import { VehiculoPublicoService } from '../../../core/services/vehiculo-publico.service';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { SeoService } from '../../../core/services/seo.service';
import { Contacto } from '../../../core/config/contacto';
import { Marca, Modelo, Opcion } from '../../../core/models/catalogo.model';
import { FiltroCatalogo, Ordenes, VehiculoPublico } from '../../../core/models/vehiculo-publico.model';
import { TarjetaVehiculoComponent } from '../comunes/tarjeta-vehiculo.component';
import { IconoComponent } from '../comunes/icono.component';

@Component({
  selector: 'app-catalogo-publico',
  standalone: true,
  imports: [FormsModule, TarjetaVehiculoComponent, IconoComponent],
  templateUrl: './catalogo.component.html',
  styleUrl: './catalogo.component.scss'
})
export class CatalogoPublicoComponent {
  private servicio = inject(VehiculoPublicoService);
  private catalogo = inject(CatalogoService);
  private ruta = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly contacto = Contacto;
  readonly ordenes = Ordenes;
  readonly POR_PAGINA = 12;

  readonly anios: number[] = (() => {
    const actual = new Date().getFullYear();
    return Array.from({ length: 16 }, (_, i) => actual - i);
  })();

  readonly precios = [15000, 20000, 25000, 30000, 40000, 50000, 70000];

  vehiculos = signal<VehiculoPublico[]>([]);
  total = signal(0);
  totalPaginas = signal(0);
  cargando = signal(true);

  marcas = signal<Marca[]>([]);
  modelos = signal<Modelo[]>([]);
  transmisiones = signal<Opcion[]>([]);
  combustibles = signal<Opcion[]>([]);

  /// El filtro vigente. Se lee de la URL, no al revés: la URL manda.
  filtro = signal<FiltroCatalogo>({});

  /// En el celular los filtros se pliegan: ocho campos arriba de la
  /// lista empujarían los vehículos fuera de la pantalla.
  filtrosAbiertos = signal(false);

  cantidadFiltros = computed(() => {
    const f = this.filtro();
    return [f.marcaId, f.modeloId, f.anioDesde, f.precioMax, f.transmision,
            f.combustible, f.aceptaFinanciamiento || undefined, f.busqueda]
      .filter(v => v !== undefined && v !== null && v !== '').length;
  });

  pagina = computed(() => this.filtro().pagina ?? 1);

  constructor() {
    inject(SeoService).setear({
      titulo: 'Catálogo de vehículos',
      descripcion:
        'Vehículos de Estados Unidos disponibles en Costa Rica, con precio final ' +
        'puesto en CR. Filtrá por marca, año, precio y financiamiento.',
      ruta: '/vehiculos'
    });

    forkJoin({
      marcas: this.catalogo.marcas(true),
      transmisiones: this.catalogo.opciones('transmision'),
      combustibles: this.catalogo.opciones('combustible')
    }).subscribe({
      next: (r) => {
        this.marcas.set(r.marcas);
        this.transmisiones.set(r.transmisiones);
        this.combustibles.set(r.combustibles);
      }
    });

    // Los filtros viven en la URL: /vehiculos?marcaId=3&precioMax=30000
    //
    // Así un cliente puede mandarle a otro "mirá, todas las Hilux bajo
    // 30 mil" y el enlace abre exactamente eso. Y el botón atrás del
    // navegador vuelve al filtro anterior en vez de salir del catálogo.
    this.ruta.queryParamMap.pipe(
      switchMap(params => {
        const f = this.leer(params);
        this.filtro.set(f);
        this.cargando.set(true);

        if (f.marcaId) this.cargarModelos(f.marcaId);
        else this.modelos.set([]);

        return this.servicio.listar({ ...f, porPagina: this.POR_PAGINA });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (p) => {
        this.vehiculos.set(p.items);
        this.total.set(p.totalRegistros);
        this.totalPaginas.set(p.totalPaginas);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  private leer(p: ParamMap): FiltroCatalogo {
    const n = (k: string) => {
      const v = Number(p.get(k));
      return Number.isFinite(v) && v > 0 ? v : undefined;
    };

    return {
      marcaId: n('marcaId'),
      modeloId: n('modeloId'),
      anioDesde: n('anioDesde'),
      precioMax: n('precioMax'),
      // 0 es un valor válido (Automática, Gasolina): no se puede usar n()
      transmision: p.has('transmision') ? Number(p.get('transmision')) : undefined,
      combustible: p.has('combustible') ? Number(p.get('combustible')) : undefined,
      aceptaFinanciamiento: p.get('financiable') === '1' || undefined,
      busqueda: p.get('q') ?? undefined,
      ordenarPor: p.get('orden') ?? undefined,
      pagina: n('pagina')
    };
  }

  /// Cambia un filtro escribiendo en la URL. La carga la dispara la
  /// suscripción de arriba: hay un solo camino para pedir vehículos.
  cambiar(cambios: Partial<Record<string, string | number | null>>): void {
    this.router.navigate([], {
      relativeTo: this.ruta,
      queryParams: { ...cambios, pagina: null },   // al filtrar, a la página 1
      queryParamsHandling: 'merge'
    });
  }

  alCambiarMarca(id: number | null): void {
    // El modelo de otra marca deja de tener sentido.
    this.cambiar({ marcaId: id, modeloId: null });
  }

  irAPagina(n: number): void {
    this.router.navigate([], {
      relativeTo: this.ruta,
      queryParams: { pagina: n > 1 ? n : null },
      queryParamsHandling: 'merge'
    });

    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  limpiar(): void {
    this.router.navigate([], { relativeTo: this.ruta, queryParams: {} });
  }

  private cargarModelos(marcaId: number): void {
    this.catalogo.modelos(marcaId).subscribe({
      next: (m) => this.modelos.set(m.filter(x => x.activo)),
      error: () => this.modelos.set([])
    });
  }

  /// Cuando no hay resultados, el mensaje de WhatsApp ya lleva lo que
  /// la persona estaba buscando. No tiene que volver a explicarlo.
  mensajeSinResultados(): string {
    const f = this.filtro();
    const marca = this.marcas().find(m => m.id === f.marcaId)?.nombre;
    const modelo = this.modelos().find(m => m.id === f.modeloId)?.nombre;

    const partes = [
      marca && modelo ? `${marca} ${modelo}` : marca,
      f.anioDesde ? `del ${f.anioDesde} en adelante` : null,
      f.precioMax ? `hasta $${f.precioMax.toLocaleString('en-US')}` : null
    ].filter(Boolean);

    const busca = partes.length ? `un ${partes.join(', ')}` : 'un vehículo';
    return this.contacto.whatsappUrl(
      `Hola, busco ${busca} y no lo vi en el catálogo. ¿Me pueden ayudar a conseguirlo?`);
  }
}
