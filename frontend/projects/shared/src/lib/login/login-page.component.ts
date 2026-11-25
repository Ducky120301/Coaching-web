import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { safeReturnUrl } from '../auth/return-url';

function messageFor(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Không kết nối được hệ thống. Vui lòng thử lại.';
    const title = (error.error as { title?: unknown } | null)?.title;
    if ([400, 401, 403, 429].includes(error.status) && typeof title === 'string') return title;
  }
  return 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';
}

// @BR-f00-codebase-004 @BR-f00-codebase-005: hiển thị thông báo từ chối do API trả về (sai web → 403).
@Component({
  selector: 'dbx-login-page',
  imports: [FormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  email = '';
  password = '';
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.email, this.password);
      await this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
    } catch (e) {
      this.error.set(messageFor(e));
    } finally {
      this.submitting.set(false);
    }
  }
}
