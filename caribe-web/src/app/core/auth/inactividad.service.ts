import { DestroyRef, Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

/// Cierra la sesion tras un rato sin actividad.
///
/// El escenario real: el dueno deja el panel abierto en una
/// computadora del taller y se va. Sin esto, la sesion sigue viva
/// siete dias —lo que dura el refresh token— y cualquiera que se
/// siente ahi ve los margenes del negocio.
@Injectable({ providedIn: 'root' })
export class InactividadService {
  private auth = inject(AuthService);
  private router = inject(Router);
  private plataforma = inject(PLATFORM_ID);
  private destroyRef = inject(DestroyRef);

  /// Treinta minutos: suficiente para redactar un vehiculo con calma
  /// sin que moleste, y corto para que un descuido no quede abierto
  /// toda la tarde.
  private readonly MINUTOS = 30;

  /// Aviso dos minutos antes, para que nadie pierda lo que estaba
  /// escribiendo sin advertencia. Cerrar sin avisar lleva a que la
  /// gente pida desactivar la funcion.
  private readonly AVISO_MINUTOS = 2;

  porExpirar = signal(false);
  segundosRestantes = signal(0);

  private temporizador: ReturnType<typeof setTimeout> | null = null;
  private cuentaRegresiva: ReturnType<typeof setInterval> | null = null;
  private activo = false;

  private readonly EVENTOS = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

  private readonly alHaberActividad = () => this.reiniciar();

  iniciar(): void {
    if (!isPlatformBrowser(this.plataforma) || this.activo) return;

    this.activo = true;

    this.EVENTOS.forEach(e =>
      window.addEventListener(e, this.alHaberActividad, { passive: true }));

    this.reiniciar();
    this.destroyRef.onDestroy(() => this.detener());
  }

  detener(): void {
    if (!isPlatformBrowser(this.plataforma)) return;

    this.activo = false;

    this.EVENTOS.forEach(e =>
      window.removeEventListener(e, this.alHaberActividad));

    this.limpiarTemporizadores();
    this.porExpirar.set(false);
  }

  /// Cualquier actividad reinicia la cuenta.
  reiniciar(): void {
    if (!this.activo) return;

    this.limpiarTemporizadores();
    this.porExpirar.set(false);

    const msHastaAviso = (this.MINUTOS - this.AVISO_MINUTOS) * 60_000;
    this.temporizador = setTimeout(() => this.avisar(), msHastaAviso);
  }

  private avisar(): void {
    this.porExpirar.set(true);
    this.segundosRestantes.set(this.AVISO_MINUTOS * 60);

    this.cuentaRegresiva = setInterval(() => {
      const quedan = this.segundosRestantes() - 1;
      this.segundosRestantes.set(quedan);

      if (quedan <= 0) this.cerrar();
    }, 1000);
  }

  private cerrar(): void {
    this.limpiarTemporizadores();
    this.porExpirar.set(false);

    this.auth.cerrarSesion().subscribe(() => {
      // El motivo viaja en la URL para que el login pueda explicar
      // por que se cerro. Sin eso, la persona vuelve al login sin
      // entender que paso.
      this.router.navigate(['/admin/login'], {
        queryParams: { motivo: 'inactividad' }
      });
    });
  }

  private limpiarTemporizadores(): void {
    if (this.temporizador) { clearTimeout(this.temporizador); this.temporizador = null; }
    if (this.cuentaRegresiva) { clearInterval(this.cuentaRegresiva); this.cuentaRegresiva = null; }
  }
}
