import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoginNoticeStore } from '../account/login-notice';
import { AuthService } from '../auth/auth.service';
import { CLIENT_APP } from '../auth/client-app';
import { safeReturnUrl } from '../auth/return-url';
import { AuthFrameComponent } from './auth-frame.component';

function messageFor(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Không kết nối được hệ thống. Vui lòng thử lại.';
    const title = (error.error as { title?: unknown } | null)?.title;
    if ([401, 403, 429].includes(error.status) && typeof title === 'string') return title;
  }
  return 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';
}

// @BR-f00-codebase-004 @BR-f00-codebase-005: hiển thị thông báo từ chối do API trả về (sai web → 403).
@Component({
  selector: 'dbx-login-page',
  imports: [FormsModule, AuthFrameComponent],
  templateUrl: './login-page.component.html',
  styleUrl: './auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly initial = inject(LoginNoticeStore).take();

  /** "Quên mật khẩu?" chỉ có ở web học viên (tech-design F01-P1 "Thiết kế giao diện (UI)"). */
  protected readonly showForgot = inject(CLIENT_APP) === 'client';

  email = '';
  password = '';
  readonly submitting = signal(false);
  readonly notice = signal<string | null>(this.initial?.tone === 'ok' ? this.initial.text : null);
  readonly error = signal<string | null>(this.initial?.tone === 'error' ? this.initial.text : null);

  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    this.notice.set(null);
    try {
      const result = await this.auth.login(this.email, this.password);
      // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm chỉ dẫn tới bước đặt mật khẩu riêng
      if (result.kind === 'activation') await this.router.navigateByUrl('/kich-hoat');
      else await this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
    } catch (e) {
      this.error.set(messageFor(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected goForgot(): void {
    void this.router.navigateByUrl('/quen-mat-khau');
  }
}
