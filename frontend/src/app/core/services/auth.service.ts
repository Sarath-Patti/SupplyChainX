import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/auth`;
  private readonly TOKEN_KEY = 'supplychainx_token';
  private readonly USER_KEY = 'supplychainx_user';

  currentUser = signal<User | null>(this.getStoredUser());
  token = signal<string | null>(this.getStoredToken());
  isInitialized = signal<boolean>(false);

  private initPromise: Promise<boolean> | null = null;

  constructor(private readonly http: HttpClient) {
    this.initializeSession();
  }

  initializeSession(): Promise<boolean> {
    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = new Promise<boolean>((resolve) => {
      const storedToken = this.getStoredToken();
      if (!storedToken) {
        this.token.set(null);
        this.currentUser.set(null);
        this.isInitialized.set(true);
        resolve(false);
        return;
      }

      this.token.set(storedToken);
      this.fetchCurrentUser().subscribe({
        next: (user) => {
          this.currentUser.set(user);
          this.isInitialized.set(true);
          resolve(true);
        },
        error: () => {
          this.logout();
          this.isInitialized.set(true);
          resolve(false);
        }
      });
    });

    return this.initPromise;
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap((res) => this.setSession(res))
    );
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data).pipe(
      tap((res) => this.setSession(res))
    );
  }

  fetchCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap((user) => {
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
    this.token.set(null);
    this.initPromise = null;
  }

  getToken(): string | null {
    return this.token();
  }

  getUser(): User | null {
    return this.currentUser();
  }

  isLoggedIn(): boolean {
    return !!this.token();
  }

  hasRole(requiredRoles: string[]): boolean {
    const user = this.currentUser();
    if (!user || !user.roles) return false;
    return requiredRoles.some(r => user.roles.includes(r));
  }

  isAdmin(): boolean {
    return this.hasRole(['Admin']);
  }

  isOperator(): boolean {
    return this.hasRole(['Operator']);
  }

  isViewer(): boolean {
    return this.hasRole(['Viewer']);
  }

  canWrite(): boolean {
    return this.hasRole(['Admin', 'Operator']);
  }

  private setSession(authResult: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, authResult.token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(authResult.user));
    this.token.set(authResult.token);
    this.currentUser.set(authResult.user);
    this.isInitialized.set(true);
  }

  private getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredUser(): User | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    if (!userStr) return null;
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }
}
