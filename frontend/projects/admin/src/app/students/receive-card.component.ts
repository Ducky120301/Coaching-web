import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Aside "Học viên sẽ nhận gì" của form Tạo hồ sơ (prototype AdTaoHoSo). */
@Component({
  selector: 'app-receive-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="receive">
      <h2>Học viên sẽ nhận gì</h2>
      <ol>
        <li>Email có link đăng nhập, email tài khoản và mật khẩu tạm.</li>
        <li>Lần đầu đăng nhập, học viên đặt mật khẩu riêng.</li>
        <li>Tài khoản chuyển sang Đã kích hoạt và mở trang Hôm nay.</li>
      </ol>
    </section>
  `,
  styles: `
    :host { display: block; }
    .receive { background: var(--soft); border-radius: 14px; padding: 16px 18px; display: flex; flex-direction: column; gap: 8px; }
    .receive h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 20px; color: var(--navInk); }
    .receive ol { margin: 0; padding-left: 20px; display: flex; flex-direction: column; gap: 6px; font-size: 14px; line-height: 1.5; }
  `,
})
export class ReceiveCardComponent {}
