import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage } from './account-errors';

/** Màn "Quên mật khẩu" — page "Main" state isForgot. */
@Component({
  selector: 'dbx-forgot-password-page',
  imports: [FormsModule, AuthFrameComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <h1>Quên mật khẩu</h1>
          <p class="lead">Nhập email bạn dùng để đăng nhập. Hệ thống gửi link đặt lại mật khẩu vào email đó.</p>
        </div>
        <form class="stack-16" (ngSubmit)="submit()">
          <div class="stack-8">
            <label for="fe">Email</label>
            <input id="fe" name="email" type="email" autocomplete="email" required
                   [ngModel]="email()" (ngModelChange)="email.set($event)">
          </div>
          @if (error(); as message) {
            <p class="error" role="alert">{{ message }}</p>
          }
          <button type="submit" class="submit tight" [disabled]="submitting()">Gửi link đặt lại</button>
        </form>
        @if (sent(); as message) {
          <p class="ok" role="status">{{ message }}</p>
        }
        <button type="button" class="back" (click)="goLogin()">Quay lại đăng nhập</button>
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal('');
  readonly sent = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  // @BR-f01-p1-tai-khoan-hoc-vien-023: luôn hiện câu chung server trả về, không phân biệt email có tài khoản hay không
  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    this.sent.set(null);
    try {
      this.sent.set(await this.auth.forgotPassword(this.email()));
    } catch (e) {
      this.error.set(accountErrorMessage(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected goLogin(): void {
    void this.router.navigateByUrl('/login');
  }
}
