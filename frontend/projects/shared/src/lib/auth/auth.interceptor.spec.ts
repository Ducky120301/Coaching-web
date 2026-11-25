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
        provideRouter([]),
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

  it('không gắn Bearer cho request ngoài API', async () => {
    const result = firstValueFrom(http.get('/config.json'));
    const req = backend.expectOne('/config.json');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
    await result;
  });
});
