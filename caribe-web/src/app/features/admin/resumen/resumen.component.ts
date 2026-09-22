import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { EstadisticaService } from '../../../core/services/estadistica.service';
import { AuthService } from '../../../core/auth/auth.service';
import { EtapaPipeline, Resumen, VentaMes } from '../../../core/models/estadistica.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';

@Component({
  selector: 'app-resumen',
  standalone: true,
  imports: [CurrencyPipe, DecimalPipe, RouterLink, EncabezadoSeccionComponent],
  templateUrl: './resumen.component.html',
  styleUrl: './resumen.component.scss'
})
export class ResumenComponent {
  private servicio = inject(EstadisticaService);
  auth = inject(AuthService);

  resumen = signal<Resumen | null>(null);
  ventas = signal<VentaMes[]>([]);
  pipeline = signal<EtapaPipeline[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  private readonly MESES = ['Ene','Feb','Mar','Abr','May','Jun','Jul','Ago','Sep','Oct','Nov','Dic'];

  /// Saludo según la hora de Costa Rica, no la del servidor.
  saludo = computed(() => {
    const h = new Date().getHours();
    const nombre = this.auth.usuario()?.nombreCompleto.split(' ')[0] ?? '';
    const parte = h < 12 ? 'Buenos días' : h < 19 ? 'Buenas tardes' : 'Buenas noches';
    return `${parte}, ${nombre}`;
  });

  mesActual = computed(() => {
    const d = new Date();
    return `${this.MESES[d.getMonth()]} ${d.getFullYear()}`;
  });

  /// Las barras se escalan contra el mes más alto. Si todos son cero,
  /// se usa 1 para no dividir por cero.
  maxIngresos = computed(() => Math.max(1, ...this.ventas().map(v => v.ingresos)));

  /// El embudo se escala contra la etapa más grande, que casi siempre
  /// es la de entrada.
  maxEtapa = computed(() => Math.max(1, ...this.pipeline().map(e => e.cantidad)));

  /// Cerradas sobre total. Descartadas cuentan en el total: una
  /// solicitud descartada también fue un cliente que no compró.
  conversion = computed(() => {
    const etapas = this.pipeline();
    const total = etapas.reduce((a, e) => a + e.cantidad, 0);
    const cerradas = etapas.find(e => e.etiqueta === 'Cerrada')?.cantidad ?? 0;
    return total > 0 ? (cerradas / total) * 100 : 0;
  });

  totalSolicitudes = computed(() =>
    this.pipeline().reduce((a, e) => a + e.cantidad, 0));

  margenPorcentaje = computed(() => {
    const r = this.resumen();
    return r && r.ingresosMes > 0 ? (r.margenMes / r.ingresosMes) * 100 : 0;
  });

  constructor() {
    forkJoin({
      resumen: this.servicio.resumen(),
      ventas: this.servicio.ventas(6),
      pipeline: this.servicio.pipeline()
    }).subscribe({
      next: (r) => {
        this.resumen.set(r.resumen);
        this.ventas.set(r.ventas);
        this.pipeline.set(r.pipeline);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar el resumen.');
        this.cargando.set(false);
      }
    });
  }

  nombreMes(v: VentaMes): string { return this.MESES[v.mes - 1]; }

  alto(valor: number, max: number): number {
    // Mínimo 2% para que un mes con ventas chicas se vea, y no parezca
    // un mes sin ventas.
    return valor > 0 ? Math.max(2, (valor / max) * 100) : 0;
  }

  /// "135 min" no se lee de un vistazo; "2 h 15 min" sí.
  tiempo(minutos: number): string {
    if (!minutos) return '—';
    if (minutos < 60) return `${Math.round(minutos)} min`;
    const h = Math.floor(minutos / 60);
    const m = Math.round(minutos % 60);
    if (h < 24) return m ? `${h} h ${m} min` : `${h} h`;
    const d = Math.floor(h / 24);
    return `${d} ${d === 1 ? 'día' : 'días'}`;
  }

  /// Menos de una hora es bueno en este negocio: el cliente que
  /// consulta un carro suele estar consultando otros al mismo tiempo.
  claseTiempo(minutos: number): string {
    if (!minutos) return '';
    if (minutos <= 60) return 'bien';
    if (minutos <= 240) return 'regular';
    return 'mal';
  }

  claseEtapa(etiqueta: string): string {
    return {
      'Nueva': 'nueva', 'Contactada': 'contactada', 'En proceso': 'proceso',
      'Cerrada': 'cerrada', 'Descartada': 'descartada'
    }[etiqueta] ?? '';
  }
}
