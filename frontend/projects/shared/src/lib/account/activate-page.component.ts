import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ActivationStore } from '../auth/activation-store';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage, problemCode } from './account-errors';
import { LoginNoticeStore } from './login-notice';
import { PasswordChecklistComponent } from './password-checklist.component';

/** Màn "Kích hoạt" — page "Main" state isActivate. */
@Component({
  selector: 'dbx-activate-page',
  imports: [FormsModule, AuthFrameComponent, PasswordChecklistComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <p class="kicker">Bước cuối để kích hoạt tài khoản</p>
          <h1>Đặt mật khẩu riêng</h1>
          <p class="lead">Mật khẩu tạm sẽ hết hiệu lực ngay khi bạn lưu. Lần sau đăng nhập bằng email và mật khẩu này.</p>
        </div>
        <form class="stack-16" (ngSubmit)="submit()">
          <div class="stack-8">
            <label for="np">Mật khẩu mới</label>
            <input id="np" name="password" type="password" autocomplete="new-password" required
                   [ngModel]="password()" (ngModelChange)="password.set($event)">
          </div>
          <div class="stack-8">
            <label for="np2">Nhập lại mật khẩu</label>
            <input id="np2" name="confirm" type="password" autocomplete="new-password" required
                   [ngModel]="confirm()" (ngModelChange)="confirm.set($event)">
          </div>
          <dbx-password-checklist [password]="password()" [confirm]="confirm()" />
          @if (error(); as message) {
            <p class="error" role="alert">{{ message }}</p>
          }
          <button type="submit" class="submit" [disabled]="submitting()">Lưu mật khẩu và vào trang Hôm nay</button>
        </form>
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivatePageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly activation = inject(ActivationStore);
  private readonly notices = inject(LoginNoticeStore);

  readonly password = signal('');
  readonly confirm = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  // @BR-f01-p1-tai-khoan-hoc-vien-016: lưu xong → tài khoản Đã kích hoạt, vào thẳng trang Hôm nay
  async submit(): Promise<void> {
    const ticket = this.activation.current();
    if (!ticket) {
      await this.router.navigateByUrl('/login');
      return;
    }
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.activate(ticket.token, this.password(), this.confirm());
      await this.router.navigateByUrl('/hom-nay');
    } catch (e) {
      // @BR-f01-p1-tai-khoan-hoc-vien-005: mã kích hoạt bị huỷ (coach gửi lại / sửa email) → đăng nhập lại bằng mật khẩu tạm mới
      if (problemCode(e) === 'auth.activation_invalid') {
        this.activation.clear();
        this.notices.set({ tone: 'error', text: accountErrorMessage(e) });
        await this.router.navigateByUrl('/login');
      } else {
        this.error.set(accountErrorMessage(e));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
