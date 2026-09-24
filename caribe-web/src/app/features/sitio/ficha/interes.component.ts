import { Component, computed, inject, input, output, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { VehiculoPublicoService } from '../../../core/services/vehiculo-publico.service';
import { Contacto } from '../../../core/config/contacto';
import {
  FormaPago, ORIGEN_FICHA, VehiculoDetalle, cuotaDe
} from '../../../core/models/vehiculo-detalle.model';

@Component({
  selector: 'app-interes',
  standalone: true,
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './interes.component.html',
  styleUrl: './interes.component.scss'
})
export class InteresComponent {
  private servicio = inject(VehiculoPublicoService);
  readonly contacto = Contacto;
  readonly PAGO = FormaPago;

  v = input.required<VehiculoDetalle>();

  /// Plazo que venía preseleccionado desde el bloque de financiamiento.
  plazoInicial = input<number | null>(null);

  cerrar = output<void>();

  nombre = signal('');
  whatsapp = signal('');
  formaPago = signal<number>(FormaPago.Contado);
  plazo = signal<number | null>(null);
  consentimiento = signal(false);

  enviando = signal(false);
  listo = signal(false);
  error = signal<string | null>(null);

  financiable = computed(() => this.v().financiamiento !== null);

  /// Cuota del plazo elegido, calculada por el servidor. Null mientras
  /// no haya plazo.
  cuota = computed(() => {
    const f = this.v().financiamiento;
    return f ? cuotaDe(f, this.plazo()) : null;
  });

  digitos = computed(() => this.whatsapp().replace(/\D/g, ''));

  valido = computed(() =>
    this.nombre().trim().length >= 3 &&
    this.digitos().length >= 8 &&
    this.consentimiento() &&
    (this.formaPago() !== FormaPago.Financiado || this.plazo() !== null));

  constructor() {
    const p = this.plazoInicial();

    if (p !== null) {
      this.formaPago.set(FormaPago.Financiado);
      this.plazo.set(p);
    }
  }

  elegirPago(valor: number): void {
    this.formaPago.set(valor);

    // Si elige financiado y hay un solo plazo, se marca solo: no tiene
    // sentido hacerle elegir entre una opción.
    if (valor === FormaPago.Financiado && this.plazo() === null) {
      const plazos = this.v().financiamiento?.plazos ?? [];
      if (plazos.length === 1) this.plazo.set(plazos[0]);
    }
  }

  enviar(): void {
    if (!this.valido() || this.enviando()) return;

    this.enviando.set(true);
    this.error.set(null);

    const veh = this.v();
    const financiado = this.formaPago() === FormaPago.Financiado;

    this.servicio.crearSolicitud({
      nombre: this.nombre().trim(),
      whatsapp: this.digitos(),
      marcaTexto: veh.marca,
      modeloTexto: veh.modelo,
      vehiculoId: veh.id,
      formaPago: this.formaPago(),
      plazoMesesInteres: financiado ? this.plazo() ?? undefined : undefined,
      origen: ORIGEN_FICHA,
      consentimiento: true
    }).subscribe({
      next: () => {
        this.enviando.set(false);
        this.listo.set(true);

        // WhatsApp se abre DESPUÉS de guardar. Así el contacto queda
        // registrado aunque la persona cierre el chat sin escribir.
        if (typeof window !== 'undefined')
          window.open(this.enlaceWhatsapp(), '_blank', 'noopener');
      },
      error: (e) => {
        this.enviando.set(false);

        this.error.set(
          e?.status === 429
            ? 'Recibimos varios envíos seguidos. Esperá un minuto, o escribinos directo por WhatsApp.'
            : e?.error?.mensaje ?? 'No pudimos registrar tu consulta. Probá escribirnos por WhatsApp.');
      }
    });
  }

  enlaceWhatsapp(): string {
    const v = this.v();
    const financiado = this.formaPago() === FormaPago.Financiado;

    const cuota = this.cuota();
    const textoCuota = cuota !== null
      ? ` (cuota estimada de $${Math.round(cuota).toLocaleString('en-US')} al mes)`
      : '';

    const pago = financiado
      ? ` con financiamiento a ${this.plazo()} meses${textoCuota}`
      : ' de contado';

    return this.contacto.whatsappUrl(
      `Hola, soy ${this.nombre().trim()}. Me interesa el ${v.marca} ${v.modelo} ` +
      `${v.anio}${pago}. ¿Me dan más información?`);
  }
}
