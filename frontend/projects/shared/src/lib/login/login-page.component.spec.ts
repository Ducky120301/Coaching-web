import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginNoticeStore } from '../account/login-notice';
import { AuthService } from '../auth/auth.service';
import { CLIENT_APP, ClientApp } from '../auth/client-app';
import { LoginResult } from '../auth/models';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  // Không dùng vi.fn() cho login: mock rejected của vi.fn() bị vitest 5 báo thành lỗi unhandled dù component đã bắt.
  let loginImpl: () => Promise<LoginResult> = () => Promise.resolve({ kind: 'session' });
  const auth = { login: (_email: string, _password: string) => loginImpl() };

  function setup(returnUrl: string | null, app: ClientApp = 'admin', before?: () => void) {
    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: CLIENT_APP, useValue: app },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) } } },
      ],
    });
    before?.();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const button = (text: string) => [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement | undefined;
    return { fixture, el, navigate, button };
  }

  beforeEach(() => { loginImpl = () => Promise.resolve({ kind: 'session' }); });

  // @BR-f00-codebase-004 @BR-f00-codebase-005 @ADR-f00-codebase-001
  it('hiển thị đúng thông báo API trả về khi bị từ chối', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({
      status: 403, error: { title: 'Tài khoản này không có quyền vào khu vực coach.' },
    }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Tài khoản này không có quyền vào khu vực coach.');
  });

  it('đăng nhập xong đi tới returnUrl an toàn', async () => {
    const { fixture, navigate } = setup('/trang-chu?tu=e2e');
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/trang-chu?tu=e2e');
  });

  it('mất kết nối thì báo tiếng Việt', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({ status: 0 }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Không kết nối được hệ thống. Vui lòng thử lại.');
  });

  // @ADR-f00-codebase-001
  it.each([500, 400])('lỗi %i thì hiện thông báo chung, không lộ tiêu đề từ API', async (status) => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({ status, error: { title: 'chi tiết nội bộ' } }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    const text = el.querySelector('[role="alert"]')?.textContent;
    expect(text).toContain('Đã có lỗi xảy ra. Vui lòng thử lại sau.');
    expect(text).not.toContain('chi tiết nội bộ');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm còn hạn → bước đặt mật khẩu riêng, không đi returnUrl
  it('đăng nhập bằng mật khẩu tạm thì chuyển sang /kich-hoat', async () => {
    loginImpl = () => Promise.resolve({ kind: 'activation', token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    const { fixture, navigate } = setup('/hom-nay', 'client');
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/kich-hoat');
    expect(navigate).not.toHaveBeenCalledWith('/hom-nay');
  });

  it('web học viên có nút "Quên mật khẩu?" mở /quen-mat-khau', () => {
    const { button, navigate } = setup(null, 'client');
    button('Quên mật khẩu?')!.click();
    expect(navigate).toHaveBeenCalledWith('/quen-mat-khau');
  });

  it('web quản trị không có nút "Quên mật khẩu?"', () => {
    const { button } = setup(null, 'admin');
    expect(button('Quên mật khẩu?')).toBeUndefined();
  });

  it('hiện câu báo thành công mang từ màn Đặt lại mật khẩu', () => {
    const { el } = setup(null, 'client', () =>
      TestBed.inject(LoginNoticeStore).set({ tone: 'ok', text: 'Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.' }));
    expect(el.querySelector('[role="status"]')?.textContent?.trim()).toBe('Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.');
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-005: mã kích hoạt bị huỷ → báo lỗi ở trang đăng nhập
  it('hiện câu lỗi mang từ màn Kích hoạt', () => {
    const { el } = setup(null, 'client', () => TestBed.inject(LoginNoticeStore).set({
      tone: 'error', text: 'Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.' }));
    expect(el.querySelector('[role="alert"]')?.textContent?.trim())
      .toBe('Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.');
  });
});
