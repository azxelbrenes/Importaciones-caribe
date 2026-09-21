import { CanDeactivateFn } from '@angular/router';

/// Lo implementa cualquier pantalla con formulario largo.
export interface ConCambios {
  tieneCambiosSinGuardar(): boolean;
}

/// Pregunta antes de salir de un formulario con cambios.
///
/// Cargar un vehiculo son veinte minutos: costos, descripcion, datos.
/// Un clic en la barra lateral por error, y se pierde todo sin aviso.
export const cambiosSinGuardarGuard: CanDeactivateFn<ConCambios> = (componente) => {
  if (!componente.tieneCambiosSinGuardar()) return true;

  return confirm(
    'Hay cambios sin guardar en este vehículo.\n\n' +
    '¿Salir de todos modos? Se van a perder.');
};
