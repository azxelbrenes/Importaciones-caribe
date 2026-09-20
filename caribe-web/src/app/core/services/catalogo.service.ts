import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Marca, Modelo, Opcion } from '../models/catalogo.model';

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/catalogo`;

  /// soloActivas=false trae tambien las desactivadas: el panel las
  /// necesita para poder reactivarlas, el sitio publico no.
  marcas(soloActivas = true): Observable<Marca[]> {
    return this.http.get<Marca[]>(
      `${this.base}/marcas?soloActivas=${soloActivas}`);
  }

  modelos(marcaId?: number): Observable<Modelo[]> {
    const url = marcaId
      ? `${this.base}/modelos?marcaId=${marcaId}`
      : `${this.base}/modelos`;

    return this.http.get<Modelo[]>(url);
  }

  crearMarca(nombre: string): Observable<number> {
    return this.http.post<number>(`${this.base}/marcas`, { nombre });
  }

  crearModelo(marcaId: number, nombre: string): Observable<number> {
    return this.http.post<number>(`${this.base}/modelos`, { marcaId, nombre });
  }

  /// tipo: transmision · combustible · traccion · estado ·
  ///       estadosolicitud · formapago
  opciones(tipo: string): Observable<Opcion[]> {
    return this.http.get<Opcion[]>(`${this.base}/opciones/${tipo}`);
  }
}
