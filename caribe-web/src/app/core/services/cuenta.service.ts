import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ConfigDobleFactor, Perfil, Sesion } from '../models/cuenta.model';

@Injectable({ providedIn: 'root' })
export class CuentaService {
  private http = inject(HttpClient);
  private auth = `${environment.apiUrl}/auth`;

  perfil(): Observable<Perfil> {
    return this.http.get<Perfil>(`${this.auth}/perfil`);
  }

  // ── Doble factor ──

  configurarDobleFactor(): Observable<ConfigDobleFactor> {
    return this.http.post<ConfigDobleFactor>(`${this.auth}/doble-factor/configurar`, {});
  }

  /// Devuelve los diez códigos de respaldo. Es la única vez que se
  /// ven completos: el servidor solo guarda su hash.
  activarDobleFactor(codigo: string): Observable<string[]> {
    return this.http.post<string[]>(`${this.auth}/doble-factor/activar`, { codigo });
  }

  regenerarCodigos(password: string): Observable<string[]> {
    return this.http.post<string[]>(`${this.auth}/doble-factor/codigos`, { password });
  }

  desactivarDobleFactor(password: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.auth}/doble-factor/desactivar`, { password });
  }

  // ── Sesiones ──

  sesiones(): Observable<Sesion[]> {
    return this.http.get<Sesion[]>(`${this.auth}/sesiones`);
  }

  cerrarOtras(): Observable<number> {
    return this.http.post<number>(`${this.auth}/sesiones/cerrar-otras`, {});
  }

  // ── Contraseña ──

  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<boolean> {
    return this.http.post<boolean>(`${environment.apiUrl}/usuarios/cambiar-password`, {
      passwordActual, passwordNueva
    });
  }
}
