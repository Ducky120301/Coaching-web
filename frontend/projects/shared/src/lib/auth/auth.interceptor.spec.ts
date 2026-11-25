import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { APP_CONFIG } from '../config/app-config';
import { authInterceptor } from './auth.interceptor';
import { CLIENT_APP } from './client-app';
import { TokenStore } from './token-store';

const tokens = (access: string, refresh: string) => ({
  accessToken: access, accessTokenExpiresAt: '', refreshToken: refresh, refreshTokenExpiresAt: '',
  user: { id: '1', email: 'a@b.vn', fullName: 'A', roles: ['Admin'] },
});

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    TestBed.inject(TokenStore).save(tokens('access-cu', 'refresh-1'));
  });

  // @BR-f00-codebase-010
  it('gặp 401 thì gia hạn một lần rồi gửi lại request gốc', async () => {
    const result = firstValueFrom(http.get<{ ok: boolean }>('/api/admin/profile'));

    const first = backend.expectOne('/api/admin/profile');
    expect(first.request.headers.get('Authorization')).toBe('Bearer access-cu');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    await Promise.resolve();
    const refresh = backend.expectOne('/api/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(tokens('access-moi', 'refresh-2'));

    await new Promise((r) => setTimeout(r));
    const retry = backend.expectOne('/api/admin/profile');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-moi');
    retry.flush({ ok: true });

    await expect(result).resolves.toEqual({ ok: true });
    expect(localStorage.getItem('dbx.admin.rt')).toBe('refresh-2');
    backend.verify();
  });

  // @BR-f00-codebase-010
  it('refresh bị từ chối 401 thì kết thúc phiên', async () => {
    const result = firstValueFrom(http.get('/api/admin/profile')).catch((e) => e);
    backend.expectOne('/api/admin/profile').flush(null, { status: 401, statusText: 'Unauthorized' });
    await Promise.resolve();
    backend.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await result;
    expect(localStorage.getItem('dbx.admin.rt')).toBeNull();
  });

  // @BR-f00-codebase-010
  it('refresh lỗi mạng thì lỗi lan ra nhưng giữ refresh token', async () => {
    const result = firstValueFrom(http.get('/api/admin/profile')).catch((e) => e);
    backend.expectOne('/api/admin/profile').flush(null, { status: 401, statusText: 'Unauthorized' });
    await Promise.resolve();
    backend.expectOne('/api/auth/refresh').error(new ProgressEvent('error'), { status: 0 });
    const err = await result;
    expect(err.status).toBe(0);
    expect(localStorage.getItem('dbx.admin.rt')).toBe('refresh-1');
  });

  // @BR-f00-codebase-010
  it('ẩn danh (không có refresh token) gặp 401 thì không gọi refresh', async () => {
    TestBed.inject(TokenStore).clear();
    const result = firstValueFrom(http.get('/api/admin/profile')).catch((e) => e);
    backend.expectOne('/api/admin/profile').flush(null, { status: 401, statusText: 'Unauthorized' });
    const err = await result;
    expect(err.status).toBe(401);
    backend.expectNone('/api/auth/refresh');
  });

  it('không gắn Bearer cho request ngoài API', async () => {
    const result = firstValueFrom(http.get('/config.json'));
    const req = backend.expectOne('/config.json');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
    await result;
  });
});
