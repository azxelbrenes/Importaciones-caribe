import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { Marca, Modelo } from '../../../core/models/catalogo.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';

@Component({
  selector: 'app-catalogo',
  standalone: true,
  imports: [FormsModule, EncabezadoSeccionComponent],
  templateUrl: './catalogo.component.html',
  styleUrl: './catalogo.component.scss'
})
export class CatalogoComponent {
  private servicio = inject(CatalogoService);

  marcas = signal<Marca[]>([]);
  modelos = signal<Modelo[]>([]);

  /// La marca cuyos modelos se están viendo.
  marcaActiva = signal<Marca | null>(null);

  cargandoMarcas = signal(true);
  cargandoModelos = signal(false);

  nuevaMarca = signal('');
  nuevoModelo = signal('');

  guardandoMarca = signal(false);
  guardandoModelo = signal(false);

  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  filtroMarca = signal('');

  /// Se filtra en el navegador y no en el servidor: con cuarenta
  /// marcas no vale la pena una petición por cada tecla. Si algún
  /// día fueran cientos, se movería al backend.
  marcasFiltradas = computed(() => {
    const texto = this.filtroMarca().trim().toLowerCase();
    if (!texto) return this.marcas();

    return this.marcas().filter(m => m.nombre.toLowerCase().includes(texto));
  });

  constructor() {
    this.cargarMarcas();
  }

  cargarMarcas(seleccionar?: number): void {
    this.cargandoMarcas.set(true);

    // soloActivas en false: el panel debe ver todo, incluidas las
    // marcas desactivadas.
    this.servicio.marcas(false).subscribe({
      next: (m) => {
        this.marcas.set(m);
        this.cargandoMarcas.set(false);

        if (seleccionar) {
          const marca = m.find(x => x.id === seleccionar);
          if (marca) this.verModelos(marca);
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

  agregarMarca(): void {
    const nombre = this.nuevaMarca().trim();
    if (nombre.length < 2 || this.guardandoMarca()) return;

    this.guardandoMarca.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    this.servicio.crearMarca(nombre).subscribe({
      next: (id) => {
        this.guardandoMarca.set(false);
        this.nuevaMarca.set('');
        this.mensaje.set(`Marca "${nombre}" agregada. Ahora cargale modelos.`);

        // Se selecciona sola: lo siguiente que va a hacer la persona
        // es agregarle el primer modelo. Obligarla a buscarla en la
        // lista sería un paso de más en el único momento en que ya
        // sabemos qué quiere hacer.
        this.cargarMarcas(id);
      },
      error: (e) => {
        this.guardandoMarca.set(false);
        // El backend responde 409 si ya existe: ese mensaje es el útil.
        this.error.set(e?.error?.mensaje ?? 'No se pudo agregar la marca.');
      }
    });
  }

  agregarModelo(): void {
    const marca = this.marcaActiva();
    const nombre = this.nuevoModelo().trim();

    if (!marca || nombre.length < 1 || this.guardandoModelo()) return;

    this.guardandoModelo.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    this.servicio.crearModelo(marca.id, nombre).subscribe({
      next: () => {
        this.guardandoModelo.set(false);
        this.nuevoModelo.set('');
        this.mensaje.set(`Modelo "${nombre}" agregado a ${marca.nombre}.`);

        this.verModelos(marca);
        this.cargarMarcas();   // actualiza el contador de modelos
      },
      error: (e) => {
        this.guardandoModelo.set(false);
        this.error.set(e?.error?.mensaje ?? 'No se pudo agregar el modelo.');
      }
    });
  }
}
