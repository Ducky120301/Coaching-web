import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  // Không dùng vi.fn() cho login: mock rejected của vi.fn() bị vitest 5 báo thành lỗi unhandled dù component đã bắt.
  let loginImpl: () => Promise<void> = () => Promise.resolve();
  const auth = { login: (_email: string, _password: string) => loginImpl() };

  function setup(returnUrl: string | null) {
    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) } } },
      ],
    });
    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => { loginImpl = () => Promise.resolve(); });

  // @BR-f00-codebase-004
  it('hiển thị đúng thông báo API trả về khi bị từ chối', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({
      status: 403, error: { title: 'Tài khoản này không có quyền vào khu vực coach.' },
    }));
    const fixture = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('Tài khoản này không có quyền vào khu vực coach.');
  });

  it('đăng nhập xong đi tới returnUrl an toàn', async () => {
    loginImpl = () => Promise.resolve();
    const fixture = setup('/trang-chu?tu=e2e');
    const router = TestBed.inject(Router);
    const nav = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    await fixture.componentInstance.submit();
    expect(nav).toHaveBeenCalledWith('/trang-chu?tu=e2e');
  });

  it('mất kết nối thì báo tiếng Việt', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({ status: 0 }));
    const fixture = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('Không kết nối được hệ thống. Vui lòng thử lại.');
  });
});
