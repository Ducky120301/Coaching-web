import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { APP_CONFIG } from '../config/app-config';
import { ActivationStore } from './activation-store';
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

describe('AuthService (web học viên)', () => {
  let auth: AuthService;
  let backend: HttpTestingController;
  const tokens = {
    accessToken: 'a-9', accessTokenExpiresAt: '', refreshToken: 'refresh-9', refreshTokenExpiresAt: '',
    user: { id: '2', email: 'quan@gmail.com', fullName: 'Phạm Hoàng Quân', roles: ['Student'] },
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'client' },
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    auth = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm → mã kích hoạt trong bộ nhớ, chưa có phiên
  it('login bằng mật khẩu tạm trả về bước kích hoạt, không lưu phiên', async () => {
    const p = auth.login('quan@gmail.com', 'Tam8K2qXyZab');
    backend.expectOne('/api/auth/login').flush({
      activationRequired: true, activationToken: 'act-1', activationTokenExpiresAt: '2026-11-25T03:00:00Z',
    });

    expect(await p).toEqual({ kind: 'activation', token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    expect(TestBed.inject(ActivationStore).current()).toEqual({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    expect(auth.user()).toBeNull();
    expect(localStorage.getItem('dbx.client.rt')).toBeNull();
  });

  it('login bằng mật khẩu riêng trả về phiên như F00', async () => {
    const p = auth.login('quan@gmail.com', 'Coach2026');
    backend.expectOne('/api/auth/login').flush(tokens);

    expect(await p).toEqual({ kind: 'session' });
    expect(auth.user()?.fullName).toBe('Phạm Hoàng Quân');
    expect(localStorage.getItem('dbx.client.rt')).toBe('refresh-9');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016: gửi nguyên văn (không trim), lưu xong vào thẳng
  it('activate gửi mã kích hoạt và hai lần nhập nguyên văn rồi vào phiên', async () => {
    const store = TestBed.inject(ActivationStore);
    store.set({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });

    const p = auth.activate('act-1', ' Coach2026 ', ' Coach2026 ');
    const request = backend.expectOne('/api/auth/activate');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ activationToken: 'act-1', password: ' Coach2026 ', confirmPassword: ' Coach2026 ' });
    request.flush(tokens);
    await p;

    expect(auth.user()).not.toBeNull();
    expect(store.current()).toBeNull();
    expect(localStorage.getItem('dbx.client.rt')).toBe('refresh-9');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-023
  it('forgotPassword trả về câu chung của server', async () => {
    const message = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';
    const p = auth.forgotPassword('QUAN@gmail.com');
    const request = backend.expectOne('/api/auth/forgot-password');
    expect(request.request.body).toEqual({ email: 'QUAN@gmail.com' });
    request.flush({ message }, { status: 202, statusText: 'Accepted' });

    expect(await p).toBe(message);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-021: đặt lại xong → phiên trên máy này cũng kết thúc
  it('resetPassword gửi token + mật khẩu và xoá phiên đang có trên máy này', async () => {
    localStorage.setItem('dbx.client.rt', 'refresh-old');
    const p = auth.resetPassword('tok-1', 'Coach2026', 'Coach2026');
    const request = backend.expectOne('/api/auth/reset-password');
    expect(request.request.body).toEqual({ token: 'tok-1', password: 'Coach2026', confirmPassword: 'Coach2026' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await p;

    expect(localStorage.getItem('dbx.client.rt')).toBeNull();
    expect(auth.user()).toBeNull();
  });
});
