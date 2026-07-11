import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth-response.model';
import { PrescriptionHubService } from './prescription-hub.service';
import { TokenStorageService } from './token-storage.service';

/** Holds the current session in a signal and keeps it in sync with localStorage. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUser = signal<AuthResponse | null>(null);

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => {
    const user = this.currentUser();
    return user !== null && new Date(user.expiresAtUtc).getTime() > Date.now();
  });

  constructor(
    private readonly http: HttpClient,
    private readonly tokenStorage: TokenStorageService,
    private readonly prescriptionHub: PrescriptionHubService,
  ) {
    this.currentUser.set(this.tokenStorage.read());

    if (this.isAuthenticated()) {
      this.prescriptionHub.start(() => this.token);
    }
  }

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, { email, password })
      .pipe(tap((auth) => this.setSession(auth)));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, { email, password })
      .pipe(tap((auth) => this.setSession(auth)));
  }

  logout(): void {
    this.tokenStorage.clear();
    this.currentUser.set(null);
    this.prescriptionHub.stop();
  }

  get token(): string | null {
    return this.currentUser()?.token ?? null;
  }

  private setSession(auth: AuthResponse): void {
    this.tokenStorage.write(auth);
    this.currentUser.set(auth);
    this.prescriptionHub.start(() => this.token);
  }
}
