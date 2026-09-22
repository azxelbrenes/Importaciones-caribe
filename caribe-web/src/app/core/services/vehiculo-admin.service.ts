import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FiltroVehiculoAdmin, VehiculoAdmin } from '../models/vehiculo-admin.model';
import { Pagina } from '../models/pagina.model';

@Injectable({ providedIn: 'root' })
export class VehiculoAdminService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/vehiculos`;
 
  listar(filtro: FiltroVehiculoAdmin = {}): Observable<Pagina<VehiculoAdmin>> {
    let params = new HttpParams();

    // Solo se envian los filtros con valor: mandar "marcaId=" vacio
    // haria que el servidor intente convertir una cadena vacia.
    Object.entries(filtro).forEach(([clave, valor]) => {
      if (valor !== undefined && valor !== null && valor !== '')
        params = params.set(clave, String(valor));
    });

    return this.http.get<Pagina<VehiculoAdmin>>(`${this.base}/admin`, { params });
  }

  cambiarEstado(id: number, nuevoEstado: number): Observable<boolean> {
    return this.http.patch<boolean>(`${this.base}/${id}/estado`, {
      id, nuevoEstado
    });
  }

  eliminar(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.base}/${id}`);
  }
}
