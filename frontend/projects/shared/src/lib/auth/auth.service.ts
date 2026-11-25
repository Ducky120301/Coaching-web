import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { CLIENT_APP } from './client-app';
import { SKIP_AUTH } from './skip-auth';
import { AuthTokens, AuthUser } from './models';
import { RefreshCoordinator } from './refresh-coordinator';
import { TokenStore } from './token-store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly app = inject(CLIENT_APP);
  private readonly store = inject(TokenStore);
  private readonly coordinator = inject(RefreshCoordinator);
  private readonly router = inject(Router);
  private readonly noAuth = { context: new HttpContext().set(SKIP_AUTH, true) };

  private readonly currentUser = signal<AuthUser | null>(null);
  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  constructor() {
    // @BR-f00-codebase-014: tab khác đăng xuất → tab này cũng kết thúc phiên
    this.coordinator.onLoggedOutElsewhere(() => this.endSession(true));
  }

  apiUrl(path: string): string {
    return `${this.config.apiBaseUrl}${path}`;
  }

  accessToken(): string | null {
    return this.store.accessToken();
  }

  async login(email: string, password: string): Promise<void> {
    const tokens = await firstValueFrom(
      this.http.post<AuthTokens>(this.apiUrl('/api/auth/login'), { email, password, app: this.app }, this.noAuth),
    );
    this.accept(tokens);
  }

  /** Khôi phục phiên khi mở lại web (@BR-f00-codebase-010). */
  async restore(): Promise<void> {
    if (!this.store.refreshToken()) return;
    try {
      await this.refresh();
    } catch {
      this.endSession(false);
    }
  }

  /** @BR-f00-codebase-010: gia hạn, chỉ 1 lần dù nhiều request/tab cùng cần. */
  refresh(): Promise<string> {
    return this.coordinator.run(async () => {
      const refreshToken = this.store.refreshToken(); // đọc lại sau khi có khóa: tab khác có thể đã đổi
      if (!refreshToken) throw new Error('Không có phiên để gia hạn');
      const tokens = await firstValueFrom(
        this.http.post<AuthTokens>(this.apiUrl('/api/auth/refresh'), { refreshToken }, this.noAuth),
      );
      this.accept(tokens);
      return tokens.accessToken;
    });
  }

  async logout(): Promise<void> {
    const refreshToken = this.store.refreshToken();
    this.endSession(false);
    this.coordinator.broadcastLogout();
    if (refreshToken) {
      try {
        await firstValueFrom(this.http.post(this.apiUrl('/api/auth/logout'), { refreshToken }, this.noAuth));
      } catch {
        /* phiên phía máy đã xoá; server tự hết hạn */
      }
    }
    await this.router.navigateByUrl('/login');
  }

  handleSessionExpired(): void {
    this.endSession(true);
  }

  private accept(tokens: AuthTokens): void {
    this.store.save(tokens);
    this.currentUser.set(tokens.user);
  }

  private endSession(navigate: boolean): void {
    this.store.clear();
    this.currentUser.set(null);
    if (navigate) void this.router.navigateByUrl('/login');
  }
}
