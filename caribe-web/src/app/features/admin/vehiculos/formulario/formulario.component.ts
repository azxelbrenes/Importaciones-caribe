import { Component, DestroyRef, HostListener, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { forkJoin } from 'rxjs';

import { VehiculoFormService } from '../../../../core/services/vehiculo-form.service';
import { CatalogoService } from '../../../../core/services/catalogo.service';
import { Marca, Modelo, Opcion } from '../../../../core/models/catalogo.model';
import { GuardarVehiculo, Simulacion } from '../../../../core/models/vehiculo-form.model';
import { ConCambios } from '../../../../core/guards/cambios-sin-guardar.guard';

import { EncabezadoSeccionComponent } from '../../comunes/encabezado-seccion.component';
import { FotosComponent } from '../fotos/fotos.component';

@Component({
  selector: 'app-vehiculo-formulario',
  standalone: true,
  imports: [
    FormsModule, RouterLink, CurrencyPipe, DecimalPipe,
    EncabezadoSeccionComponent, FotosComponent
  ],
  templateUrl: './formulario.component.html',
  styleUrl: './formulario.component.scss'
})
export class VehiculoFormularioComponent implements ConCambios {
  private servicio = inject(VehiculoFormService);
  private catalogo = inject(CatalogoService);
  private ruta = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly anios: number[] = (() => {
    const actual = new Date().getFullYear();
    const lista: number[] = [];
    for (let a = actual + 1; a >= 1980; a--) lista.push(a);
    return lista;
  })();

  id = signal<number | null>(null);
  marcas = signal<Marca[]>([]);
  modelos = signal<Modelo[]>([]);

  // Las opciones vienen del backend: si el servidor dice "Diésel" y
  // el frontend "Diesel", el cliente pregunta si son cosas distintas.
  transmisiones = signal<Opcion[]>([]);
  combustibles = signal<Opcion[]>([]);
  tracciones = signal<Opcion[]>([]);

  cargando = signal(false);
  guardando = signal(false);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  /// Si hay cambios que todavía no se guardaron.
  sucio = signal(false);

  // ── Campos ──
  marcaId       = signal<number | null>(null);
  modeloId      = signal<number | null>(null);
  anio          = signal<number>(new Date().getFullYear());
  kilometraje   = signal<number>(0);
  transmision   = signal<number>(0);
  combustible   = signal<number>(0);
  traccion      = signal<number>(0);
  color         = signal('');
  descripcion   = signal('');

  costoVehiculo  = signal<number>(0);
  costoFlete     = signal<number>(0);
  costoImpuestos = signal<number>(0);
  costoTramites  = signal<number>(0);
  honorario      = signal<number>(0);

  vigenciaDias         = signal<number>(7);
  destacado            = signal(false);
  aceptaFinanciamiento = signal(false);

  simulacion = signal<Simulacion | null>(null);

  esNuevo = computed(() => this.id() === null);

  /// El precio se calcula en vivo mientras se escriben los costos.
  ///
  /// Es una previsualización. Al guardar, el servidor recalcula desde
  /// los costos y ese es el valor real: si el frontend lo enviara,
  /// cualquiera podría manipularlo.
  costoTotal = computed(() =>
    this.num(this.costoVehiculo()) +
    this.num(this.costoFlete()) +
    this.num(this.costoImpuestos()) +
    this.num(this.costoTramites()));

  precio = computed(() => this.costoTotal() + this.num(this.honorario()));

  porcentajeMargen = computed(() => {
    const costo = this.costoTotal();
    return costo > 0 ? (this.num(this.honorario()) / costo) * 100 : 0;
  });

  valido = computed(() =>
    this.marcaId() !== null &&
    this.modeloId() !== null &&
    this.num(this.costoVehiculo()) > 0 &&
    this.num(this.honorario()) > 0);

  /// Qué falta, dicho en palabras. Un botón deshabilitado sin
  /// explicación deja a la persona adivinando.
  faltantes = computed(() => {
    const f: string[] = [];
    if (this.marcaId() === null) f.push('marca');
    if (this.modeloId() === null) f.push('modelo');
    if (this.num(this.costoVehiculo()) <= 0) f.push('costo del vehículo');
    if (this.num(this.honorario()) <= 0) f.push('honorario');
    return f;
  });

  private temporizadorSimulacion: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    forkJoin({
      marcas: this.catalogo.marcas(true),
      transmisiones: this.catalogo.opciones('transmision'),
      combustibles: this.catalogo.opciones('combustible'),
      tracciones: this.catalogo.opciones('traccion')
    }).subscribe({
      next: (r) => {
        this.marcas.set(r.marcas);
        this.transmisiones.set(r.transmisiones);
        this.combustibles.set(r.combustibles);
        this.tracciones.set(r.tracciones);
      },
      error: () => this.error.set('No pudimos cargar las opciones del formulario.')
    });

    const parametro = this.ruta.snapshot.paramMap.get('id');

    if (parametro && parametro !== 'nuevo') {
      const id = Number(parametro);
      this.id.set(id);
      this.cargarVehiculo(id);
    }

    // La simulación se pide con espera: sin eso, escribir "38000"
    // dispararía cinco consultas, una por dígito.
    effect(() => {
      const precio = this.precio();
      const acepta = this.aceptaFinanciamiento();

      if (this.temporizadorSimulacion) clearTimeout(this.temporizadorSimulacion);

      if (!acepta || precio <= 0) {
        this.simulacion.set(null);
        return;
      }

      this.temporizadorSimulacion = setTimeout(() => {
        this.servicio.simular(precio).subscribe({
          next: (s) => this.simulacion.set(s),
          error: () => this.simulacion.set(null)
        });
      }, 500);
    });

    this.destroyRef.onDestroy(() => {
      if (this.temporizadorSimulacion) clearTimeout(this.temporizadorSimulacion);
    });
  }

  /// Aviso del navegador al cerrar la pestaña con cambios. El guard
  /// cubre la navegación dentro del panel; esto cubre cerrar o recargar.
  @HostListener('window:beforeunload', ['$event'])
  alSalir(e: BeforeUnloadEvent): void {
    if (this.sucio()) e.preventDefault();
  }

  tieneCambiosSinGuardar(): boolean {
    return this.sucio();
  }

  /// Todo cambio de campo pasa por acá para marcar el formulario.
  cambiar<T>(campo: { set(v: T): void }, valor: T): void {
    campo.set(valor);
    this.sucio.set(true);
  }

  private cargarVehiculo(id: number): void {
    this.cargando.set(true);

    this.servicio.obtener(id).subscribe({
      next: (v) => {
        this.marcaId.set(v.marcaId);
        this.cargarModelos(v.marcaId, v.modeloId);

        this.anio.set(v.anio);
        this.kilometraje.set(v.kilometraje);
        this.transmision.set(v.transmision);
        this.combustible.set(v.combustible);
        this.traccion.set(v.traccion);
        this.color.set(v.color ?? '');
        this.descripcion.set(v.descripcion ?? '');

        this.costoVehiculo.set(v.costoVehiculo);
        this.costoFlete.set(v.costoFlete);
        this.costoImpuestos.set(v.costoImpuestos);
        this.costoTramites.set(v.costoTramites);
        this.honorario.set(v.honorario);

        this.vigenciaDias.set(v.vigenciaDias);
        this.destacado.set(v.destacado);
        this.aceptaFinanciamiento.set(v.aceptaFinanciamiento);

        this.cargando.set(false);
        this.sucio.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar el vehículo.');
        this.cargando.set(false);
      }
    });
  }

  alCambiarMarca(id: number | null): void {
    this.marcaId.set(id);

    // Al cambiar de marca, el modelo elegido deja de tener sentido.
    this.modeloId.set(null);
    this.modelos.set([]);
    this.sucio.set(true);

    if (id !== null) this.cargarModelos(id);
  }

  private cargarModelos(marcaId: number, seleccionar?: number): void {
    this.catalogo.modelos(marcaId).subscribe({
      next: (m) => {
        this.modelos.set(m.filter(x => x.activo || x.id === seleccionar));
        if (seleccionar) this.modeloId.set(seleccionar);
      },
      error: () => this.modelos.set([])
    });
  }

  guardar(): void {
    if (!this.valido() || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    const dto: GuardarVehiculo = {
      marcaId: this.marcaId()!,
      modeloId: this.modeloId()!,
      anio: this.anio(),
      kilometraje: this.num(this.kilometraje()),
      transmision: this.transmision(),
      combustible: this.combustible(),
      traccion: this.traccion(),
      color: this.color().trim() || undefined,
      descripcion: this.descripcion().trim() || undefined,
      costoVehiculo: this.num(this.costoVehiculo()),
      costoFlete: this.num(this.costoFlete()),
      costoImpuestos: this.num(this.costoImpuestos()),
      costoTramites: this.num(this.costoTramites()),
      honorario: this.num(this.honorario()),
      vigenciaDias: this.num(this.vigenciaDias()),
      destacado: this.destacado(),
      aceptaFinanciamiento: this.aceptaFinanciamiento()
    };

    const actual = this.id();

    if (actual === null) {
      this.servicio.crear(dto).subscribe({
        next: (nuevoId) => {
          this.guardando.set(false);
          this.sucio.set(false);
          this.id.set(nuevoId);
          this.mensaje.set('Vehículo creado. Ahora agregá las fotografías.');

          // replaceUrl: sin esto, el botón "atrás" del navegador
          // volvería al formulario vacío y se perdería el trabajo.
          this.router.navigate(['/admin/vehiculos', nuevoId], { replaceUrl: true });
        },
        error: (e) => this.fallo(e)
      });
    } else {
      this.servicio.actualizar(actual, dto).subscribe({
        next: () => {
          this.guardando.set(false);
          this.sucio.set(false);
          this.mensaje.set('Cambios guardados.');
        },
        error: (e) => this.fallo(e)
      });
    }
  }

  private fallo(e: unknown): void {
    this.guardando.set(false);

    const err = e as { error?: { mensaje?: string; errors?: Record<string, string[]> } };

    // FluentValidation devuelve un arreglo por campo. Se juntan todos
    // para corregir de una vez, en vez del ciclo de arreglar uno,
    // guardar, y descubrir el siguiente.
    if (err?.error?.errors) {
      this.error.set(Object.values(err.error.errors).flat().join(' · '));
      return;
    }

    this.error.set(err?.error?.mensaje ?? 'No se pudo guardar el vehículo.');
  }

  /// Los input numéricos devuelven cadena vacía al borrarse, y eso
  /// rompe los cálculos. Se normaliza a cero.
  private num(v: unknown): number {
    const n = Number(v);
    return Number.isFinite(n) ? n : 0;
  }
}
