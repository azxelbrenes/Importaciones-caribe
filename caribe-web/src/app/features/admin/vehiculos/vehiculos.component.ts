import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { VehiculoAdminService } from '../../../core/services/vehiculo-admin.service';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Roles } from '../../../core/auth/auth.model';
import { Marca } from '../../../core/models/catalogo.model';
import {
  ClasesEstado,
  EtiquetasEstado,
  FiltroVehiculoAdmin,
  TransicionesValidas,
  VehiculoAdmin
} from '../../../core/models/vehiculo-admin.model';

import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { ConfirmarComponent } from '../comunes/confirmar.component';

@Component({
  selector: 'app-vehiculos',
  standalone: true,
  imports: [
    CurrencyPipe, DecimalPipe, FormsModule, RouterLink,
    EncabezadoSeccionComponent, ConfirmarComponent
  ],
  templateUrl: './vehiculos.component.html',
  styleUrl: './vehiculos.component.scss'
})
export class VehiculosComponent {
  private servicio = inject(VehiculoAdminService);
  private catalogo = inject(CatalogoService);
  private auth = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  readonly etiquetas = EtiquetasEstado;
  readonly clases = ClasesEstado;

  vehiculos = signal<VehiculoAdmin[]>([]);
  marcas = signal<Marca[]>([]);

  cargando = signal(true);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  total = signal(0);
  pagina = signal(1);
  totalPaginas = signal(0);

  // ── Filtros ──
  busqueda = signal('');
  filtroEstado = signal<number | null>(null);
  filtroMarca = signal<number | null>(null);
  soloFinanciables = signal(false);

  // ── Acciones ──
  aEliminar = signal<VehiculoAdmin | null>(null);
  cambiandoEstado = signal<number | null>(null);

  /// Cuántos no se pueden publicar por falta de fotos. Se avisa arriba
  /// para que no haga falta recorrer la tabla buscándolos.
  sinFotos = computed(() =>
    this.vehiculos().filter(v => v.cantidadFotos === 0 && v.estado === 0).length);

  hayFiltros = computed(() =>
    this.busqueda().trim() !== ''
    || this.filtroEstado() !== null
    || this.filtroMarca() !== null
    || this.soloFinanciables());

  /// Solo el propietario puede borrar: es la acción más destructiva
  /// y el backend la restringe igual con [Authorize].
  puedeEliminar = computed(() =>
    this.auth.tieneAlgunRol([Roles.SuperAdministrador]));

  private temporizador: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.catalogo.marcas(false).subscribe({
      next: (m) => this.marcas.set(m),
      error: () => this.marcas.set([])
    });

    this.cargar();

    // Sin esto, si la persona cambia de pantalla mientras corre la
    // espera de la búsqueda, se dispararía sobre un componente que
    // ya no existe.
    this.destroyRef.onDestroy(() => {
      if (this.temporizador) clearTimeout(this.temporizador);
    });
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    const filtro: FiltroVehiculoAdmin = {
      busqueda: this.busqueda().trim() || undefined,
      estado: this.filtroEstado() ?? undefined,
      marcaId: this.filtroMarca() ?? undefined,
      aceptaFinanciamiento: this.soloFinanciables() || undefined,
      pagina: this.pagina(),
      porPagina: 20
    };

    this.servicio.listar(filtro).subscribe({
      next: (p) => {
        this.vehiculos.set(p.items);
        this.total.set(p.totalRegistros);
        this.totalPaginas.set(p.totalPaginas);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los vehículos.');
        this.cargando.set(false);
      }
    });
  }

  /// Al filtrar se vuelve a la página 1: quedarse en la 3 con un
  /// filtro que devuelve una sola página mostraría una lista vacía
  /// sin explicación.
  filtrar(): void {
    this.pagina.set(1);
    this.cargar();
  }

  /// Espera a que termine de escribir. Sin esto, buscar "Tacoma"
  /// dispara seis consultas, una por letra.
  buscarConEspera(): void {
    if (this.temporizador) clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => this.filtrar(), 450);
  }

  limpiarFiltros(): void {
    this.busqueda.set('');
    this.filtroEstado.set(null);
    this.filtroMarca.set(null);
    this.soloFinanciables.set(false);
    this.filtrar();
  }

  irAPagina(n: number): void {
    this.pagina.set(n);
    this.cargar();
  }

  // ══════════════ ESTADO ══════════════

  transicionesDe(estado: number): number[] {
    return TransicionesValidas[estado] ?? [];
  }

  cambiarEstado(v: VehiculoAdmin, evento: Event): void {
    const select = evento.target as HTMLSelectElement;
    const nuevo = Number(select.value);

    // Se vuelve el desplegable a "Cambiar a…" de inmediato. Si la
    // operación falla, que no quede mostrando un estado que el
    // vehículo no tiene.
    select.value = '';

    if (Number.isNaN(nuevo)) return;

    this.cambiandoEstado.set(v.id);
    this.error.set(null);
    this.mensaje.set(null);

    this.servicio.cambiarEstado(v.id, nuevo).subscribe({
      next: () => {
        this.cambiandoEstado.set(null);
        this.mensaje.set(
          `${v.marca} ${v.modelo}: ahora está en "${this.etiquetas[nuevo]}".`);
        this.cargar();
      },
      error: (e) => {
        this.cambiandoEstado.set(null);
        // El backend bloquea publicar sin fotos: ese mensaje llega
        // tal cual y es el que la persona necesita leer.
        this.error.set(e?.error?.mensaje ?? 'No se pudo cambiar el estado.');
      }
    });
  }

  // ══════════════ ELIMINAR ══════════════

  confirmarEliminar(): void {
    const v = this.aEliminar();
    if (!v) return;

    this.servicio.eliminar(v.id).subscribe({
      next: () => {
        this.aEliminar.set(null);
        this.mensaje.set(`${v.marca} ${v.modelo} fue eliminado.`);
        this.cargar();
      },
      error: (e) => {
        this.aEliminar.set(null);
        this.error.set(e?.error?.mensaje ?? 'No se pudo eliminar el vehículo.');
      }
    });
  }

  // ══════════════ AUXILIARES ══════════════

  /// El margen sobre el costo. El monto dice cuánto gana; el
  /// porcentaje dice si el trato vale la pena. $1.500 sobre $12.000
  /// no es lo mismo que sobre $60.000.
  porcentajeMargen(v: VehiculoAdmin): number {
    return v.costoTotal > 0 ? (v.margen / v.costoTotal) * 100 : 0;
  }
}
