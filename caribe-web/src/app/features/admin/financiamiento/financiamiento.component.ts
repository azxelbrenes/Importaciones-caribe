import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { FinanciamientoService } from '../../../core/services/financiamiento.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Roles } from '../../../core/auth/auth.model';
import { ConfiguracionFinanciamiento } from '../../../core/models/financiamiento.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';

interface CuotaVista {
  plazo: number;
  cuota: number;
  intereses: number;
  total: number;
}

@Component({
  selector: 'app-financiamiento',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DecimalPipe, EncabezadoSeccionComponent],
  templateUrl: './financiamiento.component.html',
  styleUrl: './financiamiento.component.scss'
})
export class FinanciamientoComponent {
  private servicio = inject(FinanciamientoService);
  private auth = inject(AuthService);

  guardado = signal<ConfiguracionFinanciamiento | null>(null);
  cargando = signal(true);
  guardando = signal(false);

  error = signal<string | null>(null);
  mensaje = signal<string | null>(null);

  // ── Campos ──
  activo = signal(false);
  prima = signal(50);
  tasa = signal(0);
  plazoMin = signal(12);
  plazoMax = signal(36);
  plazos = signal('12,24,36');
  textoLegal = signal('');

  /// Precio de ejemplo para la vista previa. Solo sirve para ver cómo
  /// quedan las cuotas antes de guardar; no se guarda.
  precioEjemplo = signal(30000);

  /// Cambiar la tasa es solo del propietario: tiene implicaciones
  /// legales y hay que poder responder por ella. El backend lo exige
  /// igual con [Authorize].
  puedeEditar = computed(() => this.auth.tieneAlgunRol([Roles.SuperAdministrador]));

  plazosLista = computed(() =>
    this.plazos()
      .split(',')
      .map(p => Number(p.trim()))
      .filter(p => Number.isInteger(p) && p > 0)
      .filter((p, i, arr) => arr.indexOf(p) === i)
      .sort((a, b) => a - b));

  /// La misma fórmula del backend —sistema francés, cuota fija—, para
  /// ver el efecto de un cambio antes de guardarlo. Al publicar, el
  /// servidor recalcula con su propia implementación.
  vistaPrevia = computed<CuotaVista[]>(() => {
    const precio = Number(this.precioEjemplo()) || 0;
    const tasa = Number(this.tasa()) || 0;

    if (precio <= 0 || tasa <= 0) return [];

    const financiado = precio - precio * ((Number(this.prima()) || 0) / 100);
    const i = tasa / 100 / 12;

    return this.plazosLista().map(n => {
      const cuota = financiado * i / (1 - Math.pow(1 + i, -n));
      const total = cuota * n;

      return {
        plazo: n,
        cuota: Math.round(cuota * 100) / 100,
        intereses: Math.round((total - financiado) * 100) / 100,
        total: Math.round((total + (precio - financiado)) * 100) / 100
      };
    });
  });

  primaEjemplo = computed(() =>
    (Number(this.precioEjemplo()) || 0) * ((Number(this.prima()) || 0) / 100));

  /// Qué impide activar, en palabras. Son las mismas dos reglas que el
  /// backend usa para rechazar el guardado.
  bloqueos = computed(() => {
    const b: string[] = [];
    if (!this.activo()) return b;
    if ((Number(this.tasa()) || 0) <= 0) b.push('falta la tasa anual');
    if (!this.textoLegal().trim()) b.push('falta el texto legal');
    return b;
  });

  errorPlazos = computed(() => {
    const lista = this.plazosLista();
    if (lista.length === 0) return 'Indicá al menos un plazo.';
    const fuera = lista.filter(p => p < this.plazoMin() || p > this.plazoMax());
    return fuera.length
      ? `Fuera del rango ${this.plazoMin()}–${this.plazoMax()}: ${fuera.join(', ')}.`
      : null;
  });

  valido = computed(() =>
    this.bloqueos().length === 0 && this.errorPlazos() === null);

  constructor() {
    this.servicio.obtener().subscribe({
      next: (c) => {
        this.guardado.set(c);
        this.activo.set(c.activo);
        this.prima.set(c.porcentajePrima);
        this.tasa.set(c.tasaAnual);
        this.plazoMin.set(c.plazoMinimoMeses);
        this.plazoMax.set(c.plazoMaximoMeses);
        this.plazos.set(c.plazosDisponibles);
        this.textoLegal.set(c.textoLegal ?? '');
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la configuración.');
        this.cargando.set(false);
      }
    });
  }

  guardar(): void {
    if (!this.valido() || this.guardando() || !this.puedeEditar()) return;

    this.guardando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    this.servicio.actualizar({
      activo: this.activo(),
      porcentajePrima: Number(this.prima()) || 0,
      tasaAnual: Number(this.tasa()) || 0,
      plazoMinimoMeses: Number(this.plazoMin()) || 12,
      plazoMaximoMeses: Number(this.plazoMax()) || 36,
      plazosDisponibles: this.plazosLista().join(','),
      textoLegal: this.textoLegal().trim() || null
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.activo()
          ? 'Guardado. Las cuotas ya se muestran en los vehículos que aceptan financiamiento.'
          : 'Guardado. El financiamiento está apagado: el sitio no muestra cuotas.');

        this.servicio.obtener().subscribe(c => this.guardado.set(c));
      },
      error: (e) => {
        this.guardando.set(false);
        const err = e?.error;
        this.error.set(err?.errors
          ? Object.values(err.errors as Record<string, string[]>).flat().join(' · ')
          : err?.mensaje ?? 'No se pudo guardar.');
      }
    });
  }

  fecha(iso: string): string {
    return new Date(iso).toLocaleString('es-CR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
