import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { APP_CONFIG } from '../config/app-config';
import { AuthService } from './auth.service';
import { CLIENT_APP } from './client-app';

const user = { id: '1', email: 'a@b.vn', fullName: 'A', roles: ['Admin'] };

describe('AuthService', () => {
  let auth: AuthService;
  let backend: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('dbx.admin.rt', 'refresh-1');
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    auth = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
  });

  // @BR-f00-codebase-010
  it('restore: refresh lỗi 503 thì giữ refresh token, chưa đăng nhập', async () => {
    const p = auth.restore();
    await new Promise((r) => setTimeout(r));
    backend.expectOne('/api/auth/refresh').flush(null, { status: 503, statusText: 'Unavailable' });
    await p;
    expect(localStorage.getItem('dbx.admin.rt')).toBe('refresh-1');
    expect(auth.user()).toBeNull();
  });

  // @BR-f00-codebase-010
  it('restore: refresh bị từ chối 401 thì xoá refresh token', async () => {
    const p = auth.restore();
    await new Promise((r) => setTimeout(r));
    backend.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await p;
    expect(localStorage.getItem('dbx.admin.rt')).toBeNull();
    expect(auth.user()).toBeNull();
  });

  // @BR-f00-codebase-014
  it('tab khác đăng xuất qua BroadcastChannel thì tab này kết thúc phiên', async () => {
    const p = auth.restore();
    await new Promise((r) => setTimeout(r));
    backend.expectOne('/api/auth/refresh').flush({
      accessToken: 'a', accessTokenExpiresAt: '', refreshToken: 'refresh-2', refreshTokenExpiresAt: '', user,
    });
    await p;
    expect(auth.user()).not.toBeNull();

    const other = new BroadcastChannel('dbx-auth-admin');
    other.postMessage('logout');
    await new Promise((r) => setTimeout(r, 50));
    other.close();

    expect(auth.user()).toBeNull();
    expect(localStorage.getItem('dbx.admin.rt')).toBeNull();
  });
});
