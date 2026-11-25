import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { formatVnDayTime, formatVnFullDate } from './format';
import { AccountEvent, AccountInfo } from './student.models';

/** Dòng trạng thái — câu theo living docs @BR-f01-p1-tai-khoan-hoc-vien-026. */
export function accountStateText(account: AccountInfo): string {
  switch (account.state) {
    case 'NotSent':
      return 'Chưa gửi thông tin đăng nhập';
    case 'Sent':
      return `Đã gửi lúc ${formatVnDayTime(account.credentialsSentAt!)}, mật khẩu tạm còn hạn đến ${formatVnDayTime(account.tempPasswordExpiresAt!)}`;
    case 'TempPasswordExpired':
      return 'Mật khẩu tạm đã hết hạn';
    case 'Activated':
      return `Đã kích hoạt ngày ${formatVnFullDate(account.activatedAt!)}`;
  }
}

/** Câu của từng dòng lịch sử (@BR-f01-p1-tai-khoan-hoc-vien-027), nguyên văn tech-design / prototype AdHoSo. */
export function accountEventText(event: AccountEvent): string {
  switch (event.type) {
    case 'CredentialsSent':
      return 'Gửi thông tin đăng nhập lần đầu.';
    case 'CredentialsResent':
      return 'Gửi lại thông tin đăng nhập. Mật khẩu tạm cũ bị huỷ.';
    case 'EmailChanged':
      return `Sửa email từ ${event.fromEmail} sang ${event.toEmail}.`;
    case 'ResetLinkSent':
      return 'Gửi link đặt lại mật khẩu.';
    case 'Activated':
      return 'Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.';
  }
}

/** Card "Tài khoản đăng nhập" — style nguyên văn tab `isTaiKhoan` của page AdHoSo. */
@Component({
  selector: 'app-account-card',
  template: `
    <section class="card" aria-labelledby="account-title">
      <h2 id="account-title">Tài khoản đăng nhập</h2>
      <p class="state">{{ stateText() }}</p>
      @if (account().history.length > 0) {
        <ol>
          @for (e of account().history; track $index) {
            <li><strong>{{ dayTime(e.at) }}</strong> {{ eventText(e) }}</li>
          }
        </ol>
      }
      <p class="foot">Mật khẩu riêng do học viên quản lý. Coach chỉ gửi được link đặt lại mật khẩu qua email.</p>
    </section>
  `,
  styles: `
    :host { display: block; }
    .card { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex;
      flex-direction: column; gap: 12px; }
    h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 24px; }
    .state { margin: 0; font-size: 14px; font-weight: 600; color: var(--ink2); }
    ol { margin: 0; padding: 0 0 0 18px; border-left: 2px solid var(--soft); list-style: none; display: flex; flex-direction: column;
      gap: 10px; font-size: 14px; }
    .foot { margin: 0; font-size: 13px; color: var(--muted); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountCardComponent {
  readonly account = input.required<AccountInfo>();
  protected readonly stateText = computed(() => accountStateText(this.account()));
  protected readonly eventText = accountEventText;
  protected readonly dayTime = formatVnDayTime;
}
