import { Injectable, inject } from '@angular/core';
import { CLIENT_APP } from './client-app';
import { AuthTokens } from './models';

// @ADR-f00-codebase-002: access token chỉ trong bộ nhớ; refresh token trong localStorage theo app
@Injectable({ providedIn: 'root' })
export class TokenStore {
  private readonly key = `dbx.${inject(CLIENT_APP)}.rt`;
  private access: string | null = null;

  accessToken(): string | null {
    return this.access;
  }

  refreshToken(): string | null {
    try {
      return localStorage.getItem(this.key);
    } catch {
      return null;
    }
  }

  save(tokens: AuthTokens): void {
    this.access = tokens.accessToken;
    try {
      localStorage.setItem(this.key, tokens.refreshToken);
    } catch {
      /* private mode: phiên chỉ sống trong tab */
    }
  }

  clear(): void {
    this.access = null;
    try {
      localStorage.removeItem(this.key);
    } catch {
      /* bỏ qua */
    }
  }
}
