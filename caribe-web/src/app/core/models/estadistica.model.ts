export interface Resumen {
  vehiculosPublicados: number;
  vehiculosEnTrato: number;
  vehiculosEnTransito: number;
  vendidosMes: number;
  solicitudesNuevas: number;
  solicitudesMes: number;
  ingresosMes: number;
  margenMes: number;
  visitasMes: number;
  diasPromedioVenta: number;
  minutosPromedioRespuesta: number;
}

export interface VentaMes {
  anio: number;
  mes: number;
  cantidad: number;
  ingresos: number;
  margen: number;
}

export interface EtapaPipeline {
  estado: number;
  etiqueta: string;
  cantidad: number;
}
