import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResultado, Roles, Token } from './auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/auth`;

  /// El token vive SOLO en memoria, nunca en localStorage ni
  /// sessionStorage.
  ///
  /// Un XSS lee esos almacenes en una linea. Una variable de
  /// JavaScript no es accesible desde otro origen y se pierde al
  /// cerrar la pestana.
  ///
  /// Al refrescar la pagina el token desaparece y se recupera con
  /// el refresh token, que viaja en una cookie HttpOnly que el
  /// script no puede leer.
  private token = signal<Token | null>(null);

  /// Si ya se intento restaurar la sesion al arrancar. El guard
  /// espera esto antes de decidir si deja pasar.
  sesionVerificada = signal(false);

  usuario = computed(() => this.token());
  autenticado = computed(() => this.token() !== null);
  roles = computed(() => this.token()?.roles ?? []);

  get accessToken(): string | null {
    return this.token()?.accessToken ?? null;
  }

  tieneAlgunRol(permitidos: string[]): boolean {
    const mios = this.roles();
    return permitidos.some(r => mios.includes(r));
  }

  /// El rol de mayor jerarquia que tenga la persona.
  rolPrincipal = computed(() => {
    const r = this.roles();
    if (r.includes(Roles.SuperAdministrador)) return 'Propietario';
    if (r.includes(Roles.Administrador)) return 'Administrador';
    if (r.includes(Roles.Operador)) return 'Operador';
    return '';
  });

  login(email: string, password: string, codigo?: string): Observable<LoginResultado> {
    return this.http.post<LoginResultado>(`${this.base}/login`, {
      email,
      password,
      codigoDobleFactor: codigo ?? null
    }, { withCredentials: true })   // sin esto la cookie no se guarda
      .pipe(tap(r => {
        if (r.token) this.token.set(r.token);
      }));
  }

  /// Pide un token nuevo usando la cookie. Lo llama el arranque de
  /// la aplicacion y el interceptor cuando recibe un 401.
  refrescar(): Observable<Token | null> {
    return this.http.post<Token>(`${this.base}/refrescar`, {},
      { withCredentials: true })
      .pipe(
        tap(t => this.token.set(t)),
        catchError(() => {
          this.token.set(null);
          return of(null);
        })
      );
  }

  /// Se ejecuta una vez al iniciar: si hay cookie valida, recupera
  /// la sesion sin pedir contrasena de nuevo.
  restaurarSesion(): Observable<boolean> {
    return this.refrescar().pipe(
      map(t => t !== null),
      tap(() => this.sesionVerificada.set(true)),
      catchError(() => {
        this.sesionVerificada.set(true);
        return of(false);
      })
    );
  }

  cerrarSesion(): Observable<unknown> {
    return this.http.post(`${this.base}/cerrar-sesion`, {},
      { withCredentials: true })
      .pipe(
        tap(() => this.token.set(null)),
        catchError(() => {
          // Aunque falle la llamada, la sesion local se limpia: la
          // persona pidio salir y tiene que salir.
          this.token.set(null);
          return of(null);
        })
      );
  }

  limpiar(): void {
    this.token.set(null);
  }

  // ── Recuperación de contraseña ──

  solicitarRecuperacion(email: string): Observable<unknown> {
    return this.http.post(`${this.base}/recuperar`, { email });
  }

  restablecer(
    token: string, passwordNueva: string, confirmarPassword: string
  ): Observable<unknown> {
    return this.http.post(`${this.base}/restablecer`, {
      token, passwordNueva, confirmarPassword
    });
  }
}
