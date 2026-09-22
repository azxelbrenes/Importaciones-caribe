import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Foto,
  GuardarVehiculo,
  ResultadoSubida,
  VehiculoDetalleAdmin
} from '../models/vehiculo-form.model';

@Injectable({ providedIn: 'root' })
export class VehiculoFormService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/vehiculos`;

  obtener(id: number): Observable<VehiculoDetalleAdmin> {
    return this.http.get<VehiculoDetalleAdmin>(`${this.base}/admin/${id}`);
  }

  crear(dto: GuardarVehiculo): Observable<number> {
    return this.http.post<number>(this.base, dto);
  }

  actualizar(id: number, dto: GuardarVehiculo): Observable<boolean> {
    return this.http.put<boolean>(`${this.base}/${id}`, { ...dto, id });
  }

  // ══════════════ FOTOS ══════════════

  listarFotos(vehiculoId: number): Observable<Foto[]> {
    return this.http.get<Foto[]>(`${this.base}/${vehiculoId}/fotos`);
  }

  /// Subida con progreso: una foto de celular por datos moviles puede
  /// tardar varios segundos, y sin barra la persona cree que se colgo.
  subirFotos(vehiculoId: number, archivos: File[]): Observable<HttpEvent<ResultadoSubida>> {
    const datos = new FormData();
    archivos.forEach(a => datos.append('archivos', a, a.name));

    const peticion = new HttpRequest<FormData>(
      'POST', `${this.base}/${vehiculoId}/fotos`, datos,
      { reportProgress: true });

    return this.http.request<ResultadoSubida>(peticion);
  }

  eliminarFoto(vehiculoId: number, fotoId: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.base}/${vehiculoId}/fotos/${fotoId}`);
  }

  marcarPortada(vehiculoId: number, fotoId: number): Observable<boolean> {
    return this.http.patch<boolean>(
      `${this.base}/${vehiculoId}/fotos/${fotoId}/portada`, {});
  }

  reordenar(vehiculoId: number, idsEnOrden: number[]): Observable<boolean> {
    return this.http.put<boolean>(`${this.base}/${vehiculoId}/fotos/orden`, {
      vehiculoId, idsEnOrden
    });
  }
}
