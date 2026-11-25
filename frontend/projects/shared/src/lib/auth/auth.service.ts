import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { CLIENT_APP } from './client-app';
import { SKIP_AUTH } from './skip-auth';
import { ActivationChallenge, AuthTokens, AuthUser, LoginResult } from './models';
import { ActivationStore } from './activation-store';
import { RefreshCoordinator } from './refresh-coordinator';
import { TokenStore } from './token-store';

export class NoSessionError extends Error {
  constructor() {
    super('Không có phiên để gia hạn');
  }
}

/** @BR-f00-codebase-010: chỉ khi server từ chối refresh token (hoặc không có token) mới coi phiên đã chết; lỗi mạng/5xx thì giữ phiên. */
export function isSessionRejected(error: unknown): boolean {
  if (error instanceof NoSessionError) return true;
  return error instanceof HttpErrorResponse && [400, 401, 403].includes(error.status);
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly app = inject(CLIENT_APP);
  private readonly store = inject(TokenStore);
  private readonly coordinator = inject(RefreshCoordinator);
  private readonly router = inject(Router);
  private readonly activation = inject(ActivationStore);
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

  async login(email: string, password: string): Promise<LoginResult> {
    const body = await firstValueFrom(
      this.http.post<AuthTokens | ActivationChallenge>(this.apiUrl('/api/auth/login'), { email, password, app: this.app }, this.noAuth),
    );
    // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm → chỉ có mã kích hoạt, chưa có phiên (không JWT, không refresh token)
    if (isActivationChallenge(body)) {
      this.activation.set({ token: body.activationToken, expiresAt: body.activationTokenExpiresAt });
      return { kind: 'activation', token: body.activationToken, expiresAt: body.activationTokenExpiresAt };
    }
    this.activation.clear();
    this.accept(body);
    return { kind: 'session' };
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-015: mật khẩu gửi nguyên văn, không trim. Thành công → API trả AuthTokens, vào thẳng. */
  async activate(activationToken: string, password: string, confirmPassword: string): Promise<void> {
    const tokens = await firstValueFrom(
      this.http.post<AuthTokens>(this.apiUrl('/api/auth/activate'), { activationToken, password, confirmPassword }, this.noAuth),
    );
    this.activation.clear();
    this.accept(tokens);
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-023: server luôn trả cùng một câu (202). */
  async forgotPassword(email: string): Promise<string> {
    const body = await firstValueFrom(
      this.http.post<{ message: string }>(this.apiUrl('/api/auth/forgot-password'), { email }, this.noAuth),
    );
    return body.message;
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-021: server thu hồi mọi phiên; phiên cục bộ (nếu có) cũng xoá để vào lại /login. */
  async resetPassword(token: string, password: string, confirmPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(this.apiUrl('/api/auth/reset-password'), { token, password, confirmPassword }, this.noAuth),
    );
    if (this.hasSession()) {
      this.endSession(false);
      this.coordinator.broadcastLogout();
    }
  }

  /** Khôi phục phiên khi mở lại web (@BR-f00-codebase-010). */
  async restore(): Promise<void> {
    if (!this.store.refreshToken()) return;
    try {
      await this.refresh();
    } catch (error) {
      // @BR-f00-codebase-010: lỗi tạm thời (mạng, 5xx) → giữ refresh token, chỉ chưa đăng nhập trong bộ nhớ
      if (isSessionRejected(error)) this.endSession(false);
    }
  }

  hasSession(): boolean {
    return this.user() !== null || this.store.refreshToken() !== null; // @BR-f00-codebase-010: tab khác có thể đã xoá token
  }

  /** @BR-f00-codebase-010: gia hạn, chỉ 1 lần dù nhiều request/tab cùng cần. */
  refresh(): Promise<string> {
    return this.coordinator.run(async () => {
      const refreshToken = this.store.refreshToken(); // đọc lại sau khi có khóa: tab khác có thể đã đổi
      if (!refreshToken) throw new NoSessionError();
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

function isActivationChallenge(body: AuthTokens | ActivationChallenge): body is ActivationChallenge {
  return (body as ActivationChallenge).activationRequired === true;
}
