import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EtapaPipeline, Resumen, VentaMes } from '../models/estadistica.model';

@Injectable({ providedIn: 'root' })
export class EstadisticaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/estadisticas`;

  resumen(): Observable<Resumen> {
    return this.http.get<Resumen>(`${this.base}/resumen`);
  }

  ventas(meses = 6): Observable<VentaMes[]> {
    return this.http.get<VentaMes[]>(`${this.base}/ventas?meses=${meses}`);
  }

  pipeline(): Observable<EtapaPipeline[]> {
    return this.http.get<EtapaPipeline[]>(`${this.base}/pipeline`);
  }
}
