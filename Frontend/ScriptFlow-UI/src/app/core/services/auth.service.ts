import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CreatedUser } from '../models/auth-response.model';
import { PrescriptionHubService } from './prescription-hub.service';
import { NotificationFeedService } from './notification-feed.service';
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
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  constructor(
    private readonly http: HttpClient,
    private readonly tokenStorage: TokenStorageService,
    private readonly prescriptionHub: PrescriptionHubService,
    // Injected (not otherwise used here) so the feed is constructed at app start and begins
    // capturing hub events immediately; also cleared on logout below.
    private readonly notificationFeed: NotificationFeedService,
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

  /** Admin-only: creates another Admin account. Unlike register()/login(), this never touches
   * the current session - the caller stays signed in as themselves, not as the new account. */
  registerAdmin(email: string, password: string): Observable<CreatedUser> {
    return this.http.post<CreatedUser>(`${environment.apiBaseUrl}/auth/register-admin`, { email, password });
  }

  logout(): void {
    this.tokenStorage.clear();
    this.currentUser.set(null);
    this.prescriptionHub.stop();
    this.notificationFeed.clear();
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
