import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { ForgotPasswordPageComponent } from './forgot-password-page.component';

const GENERIC = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';

describe('ForgotPasswordPageComponent', () => {
  let forgotImpl: () => Promise<string> = () => Promise.resolve(GENERIC);
  let emails: string[] = [];
  const auth = { forgotPassword: (email: string) => { emails.push(email); return forgotImpl(); } };

  beforeEach(() => { forgotImpl = () => Promise.resolve(GENERIC); emails = []; });

  async function setup() {
    TestBed.configureTestingModule({
      imports: [ForgotPasswordPageComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ForgotPasswordPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement, navigate };
  }

  // @BR-f01-p1-tai-khoan-hoc-vien-023: luôn hiện câu chung của server trong hộp role=status
  it('gửi email đã nhập và hiện câu chung', async () => {
    const { fixture, el } = await setup();
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Quên mật khẩu');
    const input = el.querySelector('#fe') as HTMLInputElement;
    input.value = 'QUAN@gmail.com';
    input.dispatchEvent(new Event('input'));
    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(emails).toEqual(['QUAN@gmail.com']);
    expect(el.querySelector('[role="status"]')?.textContent?.trim()).toBe(GENERIC);
  });

  it('bị giới hạn tần suất thì hiện câu của API', async () => {
    forgotImpl = () => Promise.reject(new HttpErrorResponse({ status: 429, error: { title: 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.' } }));
    const { fixture, el } = await setup();
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Bạn thao tác quá nhanh. Vui lòng thử lại sau.');
    expect(el.querySelector('[role="status"]')).toBeNull();
  });

  it('"Quay lại đăng nhập" mở /login', async () => {
    const { el, navigate } = await setup();
    ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Quay lại đăng nhập') as HTMLButtonElement).click();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
