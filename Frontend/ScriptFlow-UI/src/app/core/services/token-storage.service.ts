import { Injectable } from '@angular/core';
import { AuthResponse } from '../models/auth-response.model';

const STORAGE_KEY = 'scriptflow.auth';

/** Thin wrapper around localStorage so AuthService doesn't touch the browser API directly. */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  read(): AuthResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }

  write(auth: AuthResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
  }
}
