import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Pagina } from '../models/pagina.model';
import { FiltroCatalogo, VehiculoPublico } from '../models/vehiculo-publico.model';
import { FinanciamientoPublico } from '../models/financiamiento.model';
import { CrearSolicitud, VehiculoDetalle } from '../models/vehiculo-detalle.model';

@Injectable({ providedIn: 'root' })
export class VehiculoPublicoService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  listar(filtro: FiltroCatalogo = {}): Observable<Pagina<VehiculoPublico>> {
    let params = new HttpParams();

    Object.entries(filtro).forEach(([clave, valor]) => {
      if (valor !== undefined && valor !== null && valor !== '' && valor !== false)
        params = params.set(clave, String(valor));
    });

    return this.http.get<Pagina<VehiculoPublico>>(`${this.base}/vehiculos`, { params });
  }

  destacado(): Observable<VehiculoPublico | null> {
    return this.http.get<VehiculoPublico | null>(`${this.base}/vehiculos/destacado`);
  }

  /// La ficha. Cada visita suma una al contador que ve el panel.
  detalle(slug: string): Observable<VehiculoDetalle> {
    return this.http.get<VehiculoDetalle>(`${this.base}/vehiculos/${slug}`);
  }

  financiamiento(): Observable<FinanciamientoPublico> {
    return this.http.get<FinanciamientoPublico>(`${this.base}/financiamiento/publico`);
  }

  /// Guarda la solicitud ANTES de abrir WhatsApp. Si se abriera primero,
  /// el contacto existiría solo en el chat: sin registro, sin estadística
  /// y sin forma de saber quién quedó sin respuesta.
  crearSolicitud(dto: CrearSolicitud): Observable<number> {
    return this.http.post<number>(`${this.base}/solicitudes`, dto);
  }
}
