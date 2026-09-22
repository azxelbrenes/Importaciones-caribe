import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SolicitudService } from '../../../core/services/solicitud.service';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ROLES_GESTION, Roles } from '../../../core/auth/auth.model';
import { Opcion } from '../../../core/models/catalogo.model';
import {
  FORMA_PAGO_FINANCIADO, Solicitud, SolicitudDetalle
} from '../../../core/models/solicitud.model';

import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';
import { ConfirmarComponent } from '../comunes/confirmar.component';

@Component({
  selector: 'app-solicitudes',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, EncabezadoSeccionComponent, ConfirmarComponent],
  templateUrl: './solicitudes.component.html',
  styleUrl: './solicitudes.component.scss'
})
export class SolicitudesComponent {
  private servicio = inject(SolicitudService);
  private catalogo = inject(CatalogoService);
  private auth = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  readonly FINANCIADO = FORMA_PAGO_FINANCIADO;

  /// Los estados vienen del backend con su valor y su texto. Así no
  /// se repite el enum acá, y si mañana se agrega uno, aparece solo.
  estados = signal<Opcion[]>([]);

  solicitudes = signal<Solicitud[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  total = signal(0);
  pagina = signal(1);
  totalPaginas = signal(0);

  busqueda = signal('');
  filtroEstado = signal<number | null>(null);
  verArchivadas = signal(false);

  // ── Detalle ──
  abierta = signal<SolicitudDetalle | null>(null);
  cargandoDetalle = signal(false);
  nuevaNota = signal('');
  guardandoNota = signal(false);

  // ── Confirmaciones ──
  aEliminar = signal<SolicitudDetalle | null>(null);
  confirmarLote = signal(false);

  puedeGestionar = computed(() => this.auth.tieneAlgunRol(ROLES_GESTION));
  puedeEliminar = computed(() => this.auth.tieneAlgunRol([Roles.SuperAdministrador]));

  /// Estados por nombre. Se buscan por texto para no depender del
  /// número que tenga cada uno en el enum.
  private valorDe = (texto: string) =>
    this.estados().find(e => e.texto === texto)?.valor ?? -1;

  estadoNueva      = computed(() => this.valorDe('Nueva'));
  estadoContactada = computed(() => this.valorDe('Contactada'));
  estadoDescartada = computed(() => this.valorDe('Descartada'));
  estadoArchivada  = computed(() => this.valorDe('Archivada'));

  /// Archivada no se elige a mano: tiene su propio botón, que además
  /// fija la fecha de archivado.
  estadosElegibles = computed(() =>
    this.estados().filter(e => e.valor !== this.estadoArchivada()));

  nuevasEnPagina = computed(() =>
    this.solicitudes().filter(s => s.estado === this.estadoNueva()).length);

  private temporizador: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.catalogo.opciones('estadosolicitud').subscribe({
      next: (o) => this.estados.set(o),
      error: () => this.estados.set([])
    });

    this.cargar();

    this.destroyRef.onDestroy(() => {
      if (this.temporizador) clearTimeout(this.temporizador);
    });
  }

  cargar(): void {
    this.cargando.set(true);

    this.servicio.listar({
      busqueda: this.busqueda().trim() || undefined,
      estado: this.filtroEstado() ?? undefined,
      incluirArchivadas: this.verArchivadas() || undefined,
      pagina: this.pagina(),
      porPagina: 25
    }).subscribe({
      next: (p) => {
        this.solicitudes.set(p.items);
        this.total.set(p.totalRegistros);
        this.totalPaginas.set(p.totalPaginas);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar las solicitudes.');
        this.cargando.set(false);
      }
    });
  }

  filtrar(): void { this.pagina.set(1); this.cargar(); }

  buscarConEspera(): void {
    if (this.temporizador) clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => this.filtrar(), 450);
  }

  irAPagina(n: number): void { this.pagina.set(n); this.cargar(); }

  private avisar(texto: string): void {
    this.error.set(null);
    this.mensaje.set(texto);
  }

  private fallar(e: { error?: { mensaje?: string } }, texto: string): void {
    this.mensaje.set(null);
    this.error.set(e?.error?.mensaje ?? texto);
  }

  // ══════════════ DETALLE ══════════════

  abrir(s: Solicitud): void {
    this.cargandoDetalle.set(true);
    this.nuevaNota.set('');

    this.servicio.detalle(s.id).subscribe({
      next: (d) => { this.abierta.set(d); this.cargandoDetalle.set(false); },
      error: (e) => { this.cargandoDetalle.set(false); this.fallar(e, 'No se pudo abrir la solicitud.'); }
    });
  }

  cerrar(): void { this.abierta.set(null); }

  private refrescarAbierta(): void {
    const a = this.abierta();
    if (!a) return;
    this.servicio.detalle(a.id).subscribe(d => this.abierta.set(d));
  }

  cambiarEstado(nuevo: number): void {
    const a = this.abierta();
    if (!a || a.estado === nuevo) return;

    this.servicio.cambiarEstado(a.id, nuevo, a.asignadaA).subscribe({
      next: () => { this.refrescarAbierta(); this.cargar(); },
      error: (e) => this.fallar(e, 'No se pudo cambiar el estado.')
    });
  }

  /// Abre WhatsApp con el mensaje armado. Si la solicitud estaba
  /// Nueva, pasa sola a Contactada: es el momento real en que se la
  /// atendió, y eso alimenta el tiempo de respuesta del tablero.
  escribir(): void {
    const a = this.abierta();
    if (!a || typeof window === 'undefined') return;

    window.open(this.enlaceWhatsapp(a), '_blank', 'noopener');

    if (a.estado === this.estadoNueva() && this.estadoContactada() >= 0)
      this.cambiarEstado(this.estadoContactada());
  }

  agregarNota(): void {
    const a = this.abierta();
    const texto = this.nuevaNota().trim();
    if (!a || texto.length < 2 || this.guardandoNota()) return;

    this.guardandoNota.set(true);

    this.servicio.agregarNota(a.id, texto).subscribe({
      next: () => {
        this.guardandoNota.set(false);
        this.nuevaNota.set('');
        this.refrescarAbierta();
        this.cargar();
      },
      error: (e) => { this.guardandoNota.set(false); this.fallar(e, 'No se pudo guardar la nota.'); }
    });
  }

  archivar(): void {
    const a = this.abierta();
    if (!a) return;

    this.servicio.archivar(a.id).subscribe({
      next: () => { this.cerrar(); this.avisar(`Solicitud de ${a.nombre} archivada.`); this.cargar(); },
      error: (e) => this.fallar(e, 'No se pudo archivar.')
    });
  }

  confirmarEliminar(): void {
    const a = this.aEliminar();
    if (!a) return;

    this.servicio.eliminar(a.id).subscribe({
      next: () => {
        this.aEliminar.set(null);
        this.cerrar();
        this.avisar('Solicitud eliminada.');
        this.cargar();
      },
      error: (e) => { this.aEliminar.set(null); this.fallar(e, 'No se pudo eliminar.'); }
    });
  }

  archivarLote(): void {
    this.confirmarLote.set(false);

    this.servicio.archivarCerradas(6).subscribe({
      next: (n) => {
        this.avisar(n === 0
          ? 'No había solicitudes cerradas de más de 6 meses.'
          : `${n} ${n === 1 ? 'solicitud archivada' : 'solicitudes archivadas'}.`);
        this.cargar();
      },
      error: (e) => this.fallar(e, 'No se pudieron archivar.')
    });
  }

  /// Borrar de verdad solo para spam y duplicados, que el sistema
  /// reconoce como descartados o archivados.
  sePuedeEliminar(d: SolicitudDetalle): boolean {
    return d.estado === this.estadoDescartada() || d.estado === this.estadoArchivada();
  }

  // ══════════════ AUXILIARES ══════════════

  /// "mitsubishi-l200-2022-blanco" → "Mitsubishi L200 2022 Blanco".
  /// El slug es legible por diseño; basta con darle formato.
  vehiculo(s: { vehiculoSlug: string | null; marcaTexto: string | null; modeloTexto: string | null }): string {
    if (s.vehiculoSlug) {
      return s.vehiculoSlug
        .replace(/-\d+$/, '')
        .split('-')
        .map(p => p.length <= 3 && /\d/.test(p) ? p.toUpperCase() : p[0].toUpperCase() + p.slice(1))
        .join(' ');
    }

    const texto = [s.marcaTexto, s.modeloTexto].filter(Boolean).join(' ');
    return texto || 'Sin vehículo indicado';
  }

  pago(s: { formaPago: number; formaPagoTexto: string; plazoMesesInteres: number | null }): string {
    if (s.formaPago === this.FINANCIADO && s.plazoMesesInteres)
      return `Financiado a ${s.plazoMesesInteres} meses`;
    return s.formaPagoTexto;
  }

  /// Números de 8 dígitos son de Costa Rica y se les antepone el 506.
  /// Sin eso, WhatsApp no sabe a qué país pertenece el número.
  private numero(whatsapp: string): string {
    const d = whatsapp.replace(/\D/g, '');
    return d.length === 8 ? `506${d}` : d;
  }

  enlaceWhatsapp(d: SolicitudDetalle): string {
    const nombre = d.nombre.split(' ')[0];
    const carro = this.vehiculo(d);

    const financiado = d.formaPago === this.FINANCIADO && d.plazoMesesInteres
      ? ` con financiamiento a ${d.plazoMesesInteres} meses` : '';

    const texto =
      `Hola ${nombre}, le escribo de Importaciones del Caribe CR por el ` +
      `${carro} que consultó${financiado}. ¿Tiene un momento para conversar?`;

    return `https://wa.me/${this.numero(d.whatsapp)}?text=${encodeURIComponent(texto)}`;
  }

  claseEstado(valor: number): string {
    const texto = this.estados().find(e => e.valor === valor)?.texto ?? '';
    return {
      'Nueva': 'nueva', 'Contactada': 'contactada', 'En proceso': 'proceso',
      'Cerrada': 'cerrada', 'Descartada': 'descartada', 'Archivada': 'archivado'
    }[texto] ?? 'borrador';
  }

  textoEstado(valor: number): string {
    return this.estados().find(e => e.valor === valor)?.texto ?? '—';
  }

  /// "hace 3 h" dice más que una fecha: lo que importa en la bandeja
  /// es cuánto lleva esperando el cliente.
  hace(iso: string): string {
    const min = Math.floor((Date.now() - new Date(iso).getTime()) / 60_000);
    if (min < 1) return 'recién';
    if (min < 60) return `hace ${min} min`;
    const h = Math.floor(min / 60);
    if (h < 24) return `hace ${h} h`;
    const dias = Math.floor(h / 24);
    return dias === 1 ? 'ayer' : `hace ${dias} días`;
  }

  fecha(iso: string): string {
    return new Date(iso).toLocaleString('es-CR', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  }
}
