import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Invitacion, InvitacionCreada, PasswordRestablecida, Usuario
} from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/usuarios`;

  listar(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(this.base);
  }

  activarDesactivar(id: string, activo: boolean): Observable<boolean> {
    return this.http.patch<boolean>(`${this.base}/${id}/estado?activo=${activo}`, {});
  }

  restablecerPassword(id: string): Observable<PasswordRestablecida> {
    return this.http.post<PasswordRestablecida>(
      `${this.base}/${id}/restablecer-password`, {});
  }

  // ── Invitaciones ──

  invitar(email: string, rol: number): Observable<InvitacionCreada> {
    return this.http.post<InvitacionCreada>(`${this.base}/invitaciones`, { email, rol });
  }

  listarInvitaciones(): Observable<Invitacion[]> {
    return this.http.get<Invitacion[]>(`${this.base}/invitaciones`);
  }

  revocarInvitacion(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.base}/invitaciones/${id}`);
  }

  // ── Publico ──

  aceptarInvitacion(
    token: string, nombreCompleto: string, password: string, confirmarPassword: string
  ): Observable<boolean> {
    return this.http.post<boolean>(`${environment.apiUrl}/auth/aceptar-invitacion`, {
      token, nombreCompleto, password, confirmarPassword
    });
  }
}
