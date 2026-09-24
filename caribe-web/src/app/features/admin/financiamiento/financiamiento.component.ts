import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FinanciamientoService } from '../../../core/services/financiamiento.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Roles } from '../../../core/auth/auth.model';
import { ConfiguracionFinanciamiento } from '../../../core/models/financiamiento.model';
import { EncabezadoSeccionComponent } from '../comunes/encabezado-seccion.component';

@Component({
  selector: 'app-financiamiento',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, EncabezadoSeccionComponent],
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

  activo = signal(false);
  prima = signal(50);
  interes = signal(15);
  plazoMin = signal(12);
  plazoMax = signal(36);
  plazos = signal('12,24,36');

  /// Precio de ejemplo para la vista previa. No se guarda.
  precioEjemplo = signal(30000);

  /// Cambiar las condiciones es solo del propietario. El backend lo
  /// exige igual con [Authorize].
  puedeEditar = computed(() => this.auth.tieneAlgunRol([Roles.SuperAdministrador]));

  plazosLista = computed(() =>
    this.plazos()
      .split(',')
      .map(p => Number(p.trim()))
      .filter(p => Number.isInteger(p) && p > 0)
      .filter((p, i, arr) => arr.indexOf(p) === i)
      .sort((a, b) => a - b));

  errorPlazos = computed(() => {
    const lista = this.plazosLista();
    if (lista.length === 0) return 'Indicá al menos un plazo.';

    const fuera = lista.filter(p => p < this.plazoMin() || p > this.plazoMax());
    return fuera.length
      ? `Fuera del rango ${this.plazoMin()}–${this.plazoMax()}: ${fuera.join(', ')}.`
      : null;
  });

  errorInteres = computed(() => {
    const i = Number(this.interes());
    return Number.isFinite(i) && i >= 0 && i <= 100
      ? null
      : 'El interés debe estar entre 0 y 100.';
  });

  primaEjemplo = computed(() =>
    (Number(this.precioEjemplo()) || 0) * ((Number(this.prima()) || 0) / 100));

  /// Misma fórmula que el servidor (ConfiguracionFinanciamiento.CuotaMensual):
  /// (precio − prima) × (1 + interés%) ÷ meses, redondeado al centavo
  /// hacia arriba. Acá es solo para la vista previa; la cuota real que
  /// ve el cliente la calcula el backend.
  cuotasEjemplo = computed(() => {
    const precio = Number(this.precioEjemplo()) || 0;
    const financiado = precio - this.primaEjemplo();
    const total = financiado * (1 + (Number(this.interes()) || 0) / 100);

    return this.plazosLista().map(meses => ({
      meses,
      cuota: Math.ceil(total / meses * 100) / 100
    }));
  });

  /// El mensaje que le llega al dueño por WhatsApp, para ver cómo
  /// queda antes de activar.
  mensajeEjemplo = computed(() => {
    const c = this.cuotasEjemplo()[0];
    const plazo = c?.meses ?? 12;
    const cuota = c ? ` (cuota estimada de $${Math.round(c.cuota).toLocaleString('en-US')} al mes)` : '';
    return `Hola, me interesa el Mitsubishi L200 2022 con financiamiento ` +
           `a ${plazo} meses${cuota}. ¿Me dan más información?`;
  });

  constructor() {
    this.servicio.obtener().subscribe({
      next: (c) => {
        this.guardado.set(c);
        this.activo.set(c.activo);
        this.prima.set(c.porcentajePrima);
        this.interes.set(c.porcentajeInteres);
        this.plazoMin.set(c.plazoMinimoMeses);
        this.plazoMax.set(c.plazoMaximoMeses);
        this.plazos.set(c.plazosDisponibles);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar la configuración.');
        this.cargando.set(false);
      }
    });
  }

  guardar(): void {
    if (this.errorPlazos() || this.errorInteres() || this.guardando() || !this.puedeEditar()) return;

    this.guardando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    this.servicio.actualizar({
      activo: this.activo(),
      porcentajePrima: Number(this.prima()) || 0,
      porcentajeInteres: Number(this.interes()) || 0,
      plazoMinimoMeses: Number(this.plazoMin()) || 12,
      plazoMaximoMeses: Number(this.plazoMax()) || 36,
      plazosDisponibles: this.plazosLista().join(',')
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.activo()
          ? 'Guardado. Los vehículos financiables ya muestran los plazos y las cuotas en el sitio.'
          : 'Guardado. El financiamiento está apagado en todo el sitio.');

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
