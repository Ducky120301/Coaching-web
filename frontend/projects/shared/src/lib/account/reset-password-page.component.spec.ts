import { HttpErrorResponse } from '@angular/common/http';
import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { LoginNoticeStore } from './login-notice';
import { RESET_DONE_NOTICE, RESET_LINK_INVALID, ResetPasswordPageComponent } from './reset-password-page.component';

describe('ResetPasswordPageComponent', () => {
  let resetImpl: () => Promise<void> = () => Promise.resolve();
  let calls: unknown[][] = [];
  const auth = { resetPassword: (...args: unknown[]) => { calls.push(args); return resetImpl(); } };

  beforeEach(() => { resetImpl = () => Promise.resolve(); calls = []; });

  async function setup(fragment: string | null) {
    TestBed.configureTestingModule({
      imports: [ResetPasswordPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { fragment } } },
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const location = TestBed.inject(Location);
    vi.spyOn(location, 'path').mockReturnValue('/dat-lai-mat-khau');
    const replaceState = vi.spyOn(location, 'replaceState').mockImplementation(() => undefined);
    const fixture = TestBed.createComponent(ResetPasswordPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const type = (id: string, value: string) => {
      const input = el.querySelector(`#${id}`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, el, navigate, type, replaceState };
  }

  // @ADR-f01-p1-tai-khoan-hoc-vien-010: token đọc từ phần sau '#t=' của link
  it('đọc token từ #t=, lưu xong về /login với câu báo thành công', async () => {
    const { fixture, el, navigate, type } = await setup('t=tok-1_Ab');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Đặt lại mật khẩu');
    expect(el.querySelector('.lead')?.textContent?.trim()).toBe('Link có hiệu lực 30 phút và chỉ dùng được một lần.');
    type('rp', 'Coach2026');
    type('rp2', 'Coach2026');
    await fixture.componentInstance.submit();

    expect(calls[0]).toEqual(['tok-1_Ab', 'Coach2026', 'Coach2026']);
    expect(TestBed.inject(LoginNoticeStore).take()).toEqual({ tone: 'ok', text: RESET_DONE_NOTICE });
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020: link hỏng → hộp cảnh báo + "Xin link mới"
  it('link hết hạn hoặc đã dùng thì ẩn form, hiện câu của API và nút "Xin link mới"', async () => {
    resetImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: RESET_LINK_INVALID, code: 'auth.reset_link_invalid' } }));
    const { fixture, el, navigate } = await setup('t=tok-old');
    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe(RESET_LINK_INVALID);
    expect(el.querySelector('form')).toBeNull();
    ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Xin link mới') as HTMLButtonElement).click();
    expect(navigate).toHaveBeenCalledWith('/quen-mat-khau');
  });

  // @ADR-f01-p1-tai-khoan-hoc-vien-010: đọc token xong thì xóa '#t=…' khỏi thanh địa chỉ/lịch sử, token vẫn giữ trong component
  it('xóa token khỏi địa chỉ trang nhưng vẫn dùng được token để lưu', async () => {
    const { fixture, replaceState, type } = await setup('t=tok-2_Cd');
    expect(replaceState).toHaveBeenCalledWith('/dat-lai-mat-khau');
    type('rp', 'Coach2026');
    type('rp2', 'Coach2026');
    await fixture.componentInstance.submit();
    expect(calls[0]).toEqual(['tok-2_Cd', 'Coach2026', 'Coach2026']);
  });

  it('link không có token thì báo link hỏng ngay', async () => {
    const { el } = await setup(null);
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe(RESET_LINK_INVALID);
    expect(el.querySelector('form')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu không làm mất link — form vẫn còn
  it('hai lần nhập không khớp thì hiện câu của API, form vẫn còn', async () => {
    resetImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: 'Hai lần nhập mật khẩu không khớp.', code: 'auth.password_mismatch' } }));
    const { fixture, el } = await setup('t=tok-1');
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Hai lần nhập mật khẩu không khớp.');
    expect(el.querySelector('form')).not.toBeNull();
  });
});
