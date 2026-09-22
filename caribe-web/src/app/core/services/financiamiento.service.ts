import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConfiguracionFinanciamiento, FinanciamientoPublico, GuardarFinanciamiento
} from '../models/financiamiento.model';

@Injectable({ providedIn: 'root' })
export class FinanciamientoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/financiamiento`;

  obtener(): Observable<ConfiguracionFinanciamiento> {
    return this.http.get<ConfiguracionFinanciamiento>(`${this.base}/configuracion`);
  }

  actualizar(dto: GuardarFinanciamiento): Observable<boolean> {
    return this.http.put<boolean>(`${this.base}/configuracion`, dto);
  }

  publico(): Observable<FinanciamientoPublico> {
    return this.http.get<FinanciamientoPublico>(`${this.base}/publico`);
  }
}
