import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { Marca, Modelo } from '../../../core/models/catalogo.model';
import { AuthService } from '../../../core/auth/auth.service';
import { Roles } from '../../../core/auth/auth.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { ConfirmarComponent } from '../comunes/confirmar.component';

@Component({
  selector: 'app-catalogo',
  standalone: true,
  imports: [FormsModule, EncabezadoSeccionComponent, ConfirmarComponent],
  templateUrl: './catalogo.component.html',
  styleUrl: './catalogo.component.scss'
})
export class CatalogoComponent {
  private servicio = inject(CatalogoService);
  private auth = inject(AuthService);

  marcas = signal<Marca[]>([]);
  modelos = signal<Modelo[]>([]);
  marcaActiva = signal<Marca | null>(null);

  cargandoMarcas = signal(true);
  cargandoModelos = signal(false);

  nuevaMarca = signal('');
  nuevoModelo = signal('');

  guardandoMarca = signal(false);
  guardandoModelo = signal(false);
  limpiando = signal(false);

  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  filtroMarca = signal('');

  // ── Confirmaciones ──
  marcaAEliminar = signal<Marca | null>(null);
  modeloAEliminar = signal<Modelo | null>(null);
  confirmarLimpieza = signal(false);

  /// Eliminar es solo del propietario: es irreversible.
  puedeEliminar = computed(() =>
    this.auth.tieneAlgunRol([Roles.SuperAdministrador]));

  /// Se filtra en el navegador: con cuarenta marcas no vale la pena
  /// una petición por cada tecla.
  marcasFiltradas = computed(() => {
    const texto = this.filtroMarca().trim().toLowerCase();
    if (!texto) return this.marcas();

    return this.marcas().filter(m => m.nombre.toLowerCase().includes(texto));
  });

  constructor() {
    this.cargarMarcas();
  }

  // ══════════════════ CARGA ══════════════════

  cargarMarcas(seleccionar?: number): void {
    this.cargandoMarcas.set(true);

    // soloActivas en false: el panel debe ver todo para poder
    // reactivar lo desactivado.
    this.servicio.marcas(false).subscribe({
      next: (m) => {
        this.marcas.set(m);
        this.cargandoMarcas.set(false);

        const actual = this.marcaActiva();

        if (seleccionar) {
          const marca = m.find(x => x.id === seleccionar);
          if (marca) this.verModelos(marca);
        } else if (actual) {
          // Se refresca la referencia para que los contadores de la
          // columna derecha queden al día.
          const actualizada = m.find(x => x.id === actual.id);
          this.marcaActiva.set(actualizada ?? null);
          if (!actualizada) this.modelos.set([]);
        }
      },
      error: () => {
        this.error.set('No pudimos cargar las marcas.');
        this.cargandoMarcas.set(false);
      }
    });
  }

  verModelos(marca: Marca): void {
    this.marcaActiva.set(marca);
    this.cargandoModelos.set(true);
    this.nuevoModelo.set('');

    this.servicio.modelos(marca.id).subscribe({
      next: (m) => { this.modelos.set(m); this.cargandoModelos.set(false); },
      error: () => { this.modelos.set([]); this.cargandoModelos.set(false); }
    });
  }

  // ══════════════════ CREAR ══════════════════

  agregarMarca(): void {
    const nombre = this.nuevaMarca().trim();
    if (nombre.length < 2 || this.guardandoMarca()) return;

    this.guardandoMarca.set(true);
    this.limpiarAvisos();

    this.servicio.crearMarca(nombre).subscribe({
      next: (id) => {
        this.guardandoMarca.set(false);
        this.nuevaMarca.set('');
        this.mensaje.set(`Marca "${nombre}" agregada. Ahora cargale modelos.`);

        // Se selecciona sola: lo siguiente que va a hacer la persona
        // es agregarle el primer modelo.
        this.cargarMarcas(id);
      },
      error: (e) => {
        this.guardandoMarca.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo agregar la marca.');
      }
    });
  }

  agregarModelo(): void {
    const marca = this.marcaActiva();
    const nombre = this.nuevoModelo().trim();

    if (!marca || nombre.length < 1 || this.guardandoModelo()) return;

    this.guardandoModelo.set(true);
    this.limpiarAvisos();

    this.servicio.crearModelo(marca.id, nombre).subscribe({
      next: () => {
        this.guardandoModelo.set(false);
        this.nuevoModelo.set('');
        this.mensaje.set(`Modelo "${nombre}" agregado a ${marca.nombre}.`);

        this.verModelos(marca);
        this.cargarMarcas();
      },
      error: (e) => {
        this.guardandoModelo.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo agregar el modelo.');
      }
    });
  }

  // ══════════════════ DESACTIVAR ══════════════════

  alternarMarca(m: Marca, evento: Event): void {
    // La fila entera abre los modelos; este botón no debe dispararlo.
    evento.stopPropagation();
    this.limpiarAvisos();

    this.servicio.activarMarca(m.id, !m.activa).subscribe({
      next: () => {
        this.mensaje.set(m.activa
          ? `${m.nombre} y sus modelos quedaron ocultos del catálogo.`
          : `${m.nombre} vuelve a estar disponible.`);

        this.cargarMarcas();

        if (this.marcaActiva()?.id === m.id) this.verModelos(m);
      },
      error: () => this.error.set('No se pudo cambiar el estado.')
    });
  }

  alternarModelo(m: Modelo): void {
    this.limpiarAvisos();

    this.servicio.activarModelo(m.id, !m.activo).subscribe({
      next: () => {
        const marca = this.marcaActiva();
        if (marca) this.verModelos(marca);
        this.cargarMarcas();
      },
      error: () => this.error.set('No se pudo cambiar el estado.')
    });
  }

  // ══════════════════ ELIMINAR ══════════════════

  pedirEliminarMarca(m: Marca, evento: Event): void {
    evento.stopPropagation();
    this.marcaAEliminar.set(m);
  }

  confirmarEliminarMarca(): void {
    const m = this.marcaAEliminar();
    if (!m) return;

    this.servicio.eliminarMarca(m.id).subscribe({
      next: () => {
        this.marcaAEliminar.set(null);
        this.mensaje.set(`${m.nombre} fue eliminada.`);

        if (this.marcaActiva()?.id === m.id) {
          this.marcaActiva.set(null);
          this.modelos.set([]);
        }

        this.cargarMarcas();
      },
      error: (e) => {
        this.marcaAEliminar.set(null);
        // El backend explica cuántos vehículos la usan: ese mensaje
        // es más útil que uno genérico.
        this.error.set(e?.error?.mensaje ?? 'No se pudo eliminar la marca.');
      }
    });
  }

  confirmarEliminarModelo(): void {
    const m = this.modeloAEliminar();
    if (!m) return;

    this.servicio.eliminarModelo(m.id).subscribe({
      next: () => {
        this.modeloAEliminar.set(null);
        this.mensaje.set(`${m.nombre} fue eliminado.`);

        const marca = this.marcaActiva();
        if (marca) this.verModelos(marca);
        this.cargarMarcas();
      },
      error: (e) => {
        this.modeloAEliminar.set(null);
        this.error.set(e?.error?.mensaje ?? 'No se pudo eliminar el modelo.');
      }
    });
  }

  // ══════════════════ LIMPIEZA ══════════════════

  ejecutarLimpieza(): void {
    this.confirmarLimpieza.set(false);
    this.limpiando.set(true);
    this.limpiarAvisos();

    this.servicio.limpiarDuplicados().subscribe({
      next: (r) => {
        this.limpiando.set(false);

        const total = r.marcasFusionadas + r.modelosFusionados + r.nombresCorregidos;

        this.mensaje.set(total === 0
          ? 'No se encontraron duplicados. El catálogo está limpio.'
          : `Listo: ${r.marcasFusionadas} marcas y ${r.modelosFusionados} ` +
            `modelos fusionados, ${r.nombresCorregidos} nombres corregidos.`);

        this.marcaActiva.set(null);
        this.modelos.set([]);
        this.cargarMarcas();
      },
      error: (e) => {
        this.limpiando.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo limpiar el catálogo.');
      }
    });
  }

  private limpiarAvisos(): void {
    this.error.set(null);
    this.mensaje.set(null);
  }
}
