import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActivationStore, activationGuard } from '../auth/activation-store';
import { AuthService } from '../auth/auth.service';
import { ActivatePageComponent } from './activate-page.component';
import { LoginNoticeStore } from './login-notice';

describe('ActivatePageComponent', () => {
  let activateImpl: () => Promise<void> = () => Promise.resolve();
  let calls: unknown[][] = [];
  const auth = { activate: (...args: unknown[]) => { calls.push(args); return activateImpl(); } };

  beforeEach(() => { activateImpl = () => Promise.resolve(); calls = []; });

  async function setup() {
    TestBed.configureTestingModule({
      imports: [ActivatePageComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
    TestBed.inject(ActivationStore).set({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ActivatePageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const type = (id: string, value: string) => {
      const input = el.querySelector(`#${id}`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, el, navigate, type };
  }

  it('đúng câu chữ page Main (isActivate)', async () => {
    const { el } = await setup();
    expect(el.querySelector('.kicker')?.textContent?.trim()).toBe('Bước cuối để kích hoạt tài khoản');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Đặt mật khẩu riêng');
    expect(el.querySelector('label[for="np"]')?.textContent?.trim()).toBe('Mật khẩu mới');
    expect(el.querySelector('label[for="np2"]')?.textContent?.trim()).toBe('Nhập lại mật khẩu');
    expect(el.querySelector('button[type="submit"]')?.textContent?.trim()).toBe('Lưu mật khẩu và vào trang Hôm nay');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: tick khi đạt, đếm đúng từng ký tự
  it('checklist tick khi đủ 8 ký tự và hai lần nhập trùng nhau', async () => {
    const { el, type } = await setup();
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(0);
    type('np', 'abcdefg');
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(0);
    type('np', 'abcdefgh');
    type('np2', 'abcdefgh');
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(2);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016: gửi nguyên văn, lưu xong vào trang Hôm nay
  it('lưu mật khẩu nguyên văn rồi vào /hom-nay', async () => {
    const { fixture, navigate, type } = await setup();
    type('np', ' Coach2026 ');
    type('np2', ' Coach2026 ');
    await fixture.componentInstance.submit();
    expect(calls[0]).toEqual(['act-1', ' Coach2026 ', ' Coach2026 ']);
    expect(navigate).toHaveBeenCalledWith('/hom-nay');
  });

  it('mật khẩu không hợp lệ thì hiện câu của API, ở lại trang', async () => {
    activateImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: 'Mật khẩu cần ít nhất 8 ký tự.', code: 'auth.password_too_short' } }));
    const { fixture, el, navigate } = await setup();
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Mật khẩu cần ít nhất 8 ký tự.');
    expect(navigate).not.toHaveBeenCalled();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-005: coach gửi lại giữa chừng → mã kích hoạt bị huỷ → đăng nhập lại
  it('mã kích hoạt bị huỷ thì về /login kèm câu lỗi', async () => {
    const title = 'Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.';
    activateImpl = () => Promise.reject(new HttpErrorResponse({ status: 401, error: { title, code: 'auth.activation_invalid' } }));
    const { fixture, navigate } = await setup();
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/login');
    expect(TestBed.inject(ActivationStore).current()).toBeNull();
    expect(TestBed.inject(LoginNoticeStore).take()).toEqual({ tone: 'error', text: title });
  });

  it('vào /kich-hoat mà không có mã kích hoạt thì về /login', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const result = TestBed.runInInjectionContext(() => activationGuard({} as never, {} as never));
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
