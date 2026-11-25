import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage, problemCode } from './account-errors';
import { LoginNoticeStore } from './login-notice';
import { PasswordChecklistComponent } from './password-checklist.component';

export const RESET_DONE_NOTICE = 'Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.';
export const RESET_LINK_INVALID = 'Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.';

/**
 * Màn "Đặt lại mật khẩu" (suy ra — dùng lại bố cục Kích hoạt của page "Main").
 * @ADR-f01-p1-tai-khoan-hoc-vien-010: link `<ClientBaseUrl>/dat-lai-mat-khau#t=<token>` — token sau '#' không lên server,
 * không lọt vào Referer hay log.
 */
@Component({
  selector: 'dbx-reset-password-page',
  imports: [FormsModule, AuthFrameComponent, PasswordChecklistComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <h1>Đặt lại mật khẩu</h1>
          <p class="lead">Link có hiệu lực 30 phút và chỉ dùng được một lần.</p>
        </div>
        @if (linkError(); as message) {
          <p class="error" role="alert">{{ message }}</p>
          <button type="button" class="back" (click)="requestNewLink()">Xin link mới</button>
        } @else {
          <form class="stack-16" (ngSubmit)="submit()">
            <div class="stack-8">
              <label for="rp">Mật khẩu mới</label>
              <input id="rp" name="password" type="password" autocomplete="new-password" required
                     [ngModel]="password()" (ngModelChange)="password.set($event)">
            </div>
            <div class="stack-8">
              <label for="rp2">Nhập lại mật khẩu</label>
              <input id="rp2" name="confirm" type="password" autocomplete="new-password" required
                     [ngModel]="confirm()" (ngModelChange)="confirm.set($event)">
            </div>
            <dbx-password-checklist [password]="password()" [confirm]="confirm()" />
            @if (error(); as message) {
              <p class="error" role="alert">{{ message }}</p>
            }
            <button type="submit" class="submit" [disabled]="submitting()">Lưu mật khẩu mới</button>
          </form>
        }
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notices = inject(LoginNoticeStore);
  private readonly token = new URLSearchParams(inject(ActivatedRoute).snapshot.fragment ?? '').get('t');

  readonly password = signal('');
  readonly confirm = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly linkError = signal<string | null>(this.token ? null : RESET_LINK_INVALID);

  async submit(): Promise<void> {
    if (!this.token || this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.resetPassword(this.token, this.password(), this.confirm());
      this.notices.set({ tone: 'ok', text: RESET_DONE_NOTICE });
      await this.router.navigateByUrl('/login');
    } catch (e) {
      // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019: link hỏng → xin link mới; lỗi mật khẩu giữ link
      if (problemCode(e) === 'auth.reset_link_invalid') this.linkError.set(accountErrorMessage(e));
      else this.error.set(accountErrorMessage(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected requestNewLink(): void {
    void this.router.navigateByUrl('/quen-mat-khau');
  }
}
