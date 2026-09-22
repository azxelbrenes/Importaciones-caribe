import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Pagina } from '../models/pagina.model';
import { FiltroSolicitud, Solicitud, SolicitudDetalle } from '../models/solicitud.model';

@Injectable({ providedIn: 'root' })
export class SolicitudService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/solicitudes`;

  listar(filtro: FiltroSolicitud = {}): Observable<Pagina<Solicitud>> {
    let params = new HttpParams();

    Object.entries(filtro).forEach(([clave, valor]) => {
      if (valor !== undefined && valor !== null && valor !== '')
        params = params.set(clave, String(valor));
    });

    return this.http.get<Pagina<Solicitud>>(this.base, { params });
  }

  detalle(id: number): Observable<SolicitudDetalle> {
    return this.http.get<SolicitudDetalle>(`${this.base}/${id}`);
  }

  /// asignadaAId se reenvía tal cual: el backend reemplaza el valor
  /// con lo que llegue, y mandarlo vacío borraría la asignación.
  cambiarEstado(id: number, estado: number, asignadaAId: string | null): Observable<boolean> {
    return this.http.put<boolean>(`${this.base}/${id}`, { id, estado, asignadaAId });
  }

  agregarNota(id: number, nota: string): Observable<number> {
    return this.http.post<number>(`${this.base}/${id}/notas`, { solicitudId: id, nota });
  }

  archivar(id: number): Observable<boolean> {
    return this.http.post<boolean>(`${this.base}/${id}/archivar`, {});
  }

  archivarCerradas(meses: number): Observable<number> {
    return this.http.post<number>(`${this.base}/archivar-cerradas?meses=${meses}`, {});
  }

  eliminar(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.base}/${id}`);
  }
}
