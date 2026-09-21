import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { VehiculoFormService } from '../../../../core/services/vehiculo-form.service';
import { Foto } from '../../../../core/models/vehiculo-form.model';

@Component({
  selector: 'app-fotos',
  standalone: true,
  templateUrl: './fotos.component.html',
  styleUrl: './fotos.component.scss'
})
export class FotosComponent {
  private servicio = inject(VehiculoFormService);

  vehiculoId = input.required<number>();

  fotos = signal<Foto[]>([]);
  cargando = signal(true);
  subiendo = signal(false);
  progreso = signal(0);
  error = signal<string | null>(null);
  arrastrando = signal(false);

  /// El mismo límite que valida el backend.
  readonly MAXIMO = 12;

  puedeSubir = computed(() => this.fotos().length < this.MAXIMO);
  restantes = computed(() => this.MAXIMO - this.fotos().length);

  constructor() {
    // Se recarga cuando cambia el id. Al crear un vehículo nuevo, el
    // componente se monta recién después de guardar, así que el id
    // llega una vez y esto corre una vez.
    effect(() => {
      const id = this.vehiculoId();
      if (id) this.cargar();
    });
  }

  cargar(): void {
    this.cargando.set(true);

    this.servicio.listarFotos(this.vehiculoId()).subscribe({
      next: (f) => { this.fotos.set(f); this.cargando.set(false); },
      error: () => { this.fotos.set([]); this.cargando.set(false); }
    });
  }

  alSeleccionar(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    if (!input.files?.length) return;

    this.subir(Array.from(input.files));

    // Sin esto, elegir el mismo archivo dos veces seguidas no dispara
    // el evento la segunda vez.
    input.value = '';
  }

  // ── Arrastrar y soltar ──

  alArrastrar(e: DragEvent): void {
    e.preventDefault();
    this.arrastrando.set(true);
  }

  alSalir(e: DragEvent): void {
    e.preventDefault();
    this.arrastrando.set(false);
  }

  alSoltar(e: DragEvent): void {
    e.preventDefault();
    this.arrastrando.set(false);

    const archivos = Array.from(e.dataTransfer?.files ?? [])
      .filter(a => a.type.startsWith('image/'));

    if (archivos.length > 0) this.subir(archivos);
  }

  private subir(archivos: File[]): void {
    if (this.subiendo()) return;

    const disponibles = this.restantes();

    if (disponibles <= 0) {
      this.error.set(`Máximo ${this.MAXIMO} fotografías por vehículo.`);
      return;
    }

    // Se recorta al cupo en vez de rechazar todo: si alguien arrastra
    // quince fotos y caben tres, entran las tres.
    const aSubir = archivos.slice(0, disponibles);
    const sobrantes = archivos.length - aSubir.length;

    this.subiendo.set(true);
    this.progreso.set(0);
    this.error.set(null);

    this.servicio.subirFotos(this.vehiculoId(), aSubir).subscribe({
      next: (evento) => {
        if (evento.type === HttpEventType.UploadProgress && evento.total) {
          this.progreso.set(Math.round((evento.loaded / evento.total) * 100));
        }

        if (evento.type === HttpEventType.Response) {
          this.subiendo.set(false);
          this.progreso.set(0);

          const r = evento.body;
          const avisos: string[] = [];

          // El backend responde por archivo: se muestra cuál falló y
          // por qué, en vez de un "algo salió mal" genérico.
          if (r?.errores?.length)
            avisos.push(...r.errores.map(e => `${e.archivo}: ${e.motivo}`));

          if (sobrantes > 0)
            avisos.push(`${sobrantes} no entraron por el límite de ${this.MAXIMO}.`);

          if (avisos.length) this.error.set(avisos.join(' · '));

          this.cargar();
        }
      },
      error: () => {
        this.subiendo.set(false);
        this.progreso.set(0);
        this.error.set('No se pudieron subir las fotografías.');
      }
    });
  }

  eliminar(f: Foto): void {
    this.error.set(null);

    this.servicio.eliminarFoto(this.vehiculoId(), f.id).subscribe({
      next: () => this.cargar(),
      // El backend bloquea dejar sin fotos un vehículo publicado: ese
      // mensaje es el que la persona necesita.
      error: (e) => this.error.set(
        e?.error?.mensaje ?? 'No se pudo eliminar la fotografía.')
    });
  }

  marcarPortada(f: Foto): void {
    if (f.esPortada) return;
    this.error.set(null);

    this.servicio.marcarPortada(this.vehiculoId(), f.id).subscribe({
      next: () => this.cargar(),
      error: () => this.error.set('No se pudo cambiar la portada.')
    });
  }

  /// Reordenar con flechas en vez de arrastrar: en un celular,
  /// arrastrar una miniatura dentro de una rejilla es frustrante, y
  /// las flechas funcionan igual con el dedo y con el teclado.
  mover(indice: number, direccion: -1 | 1): void {
    const lista = [...this.fotos()];
    const destino = indice + direccion;

    if (destino < 0 || destino >= lista.length) return;

    [lista[indice], lista[destino]] = [lista[destino], lista[indice]];

    // Se muestra el cambio de inmediato y se guarda después. Si falla,
    // se vuelve al orden real del servidor.
    this.fotos.set(lista);

    this.servicio.reordenar(this.vehiculoId(), lista.map(f => f.id)).subscribe({
      error: () => {
        this.error.set('No se pudo guardar el orden.');
        this.cargar();
      }
    });
  }
}
