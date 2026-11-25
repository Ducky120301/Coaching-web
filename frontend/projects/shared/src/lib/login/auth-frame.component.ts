import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Bìa thương hiệu + khung form của page "Main" (đăng nhập, kích hoạt, quên / đặt lại mật khẩu). */
@Component({
  selector: 'dbx-auth-frame',
  template: `
    <div class="page">
      <section class="cover" aria-label="Thương hiệu">
        <p class="domain">Unfiltered Coaching Team</p>
        <div class="cover-mid">
          <p class="sig" aria-hidden="true">duobexiuuu</p>
          <p class="headline cover-type">online<br>physique<br>coaching</p>
        </div>
        <div class="cover-foot">
          <span>@duobexiuuu/instagram</span>
          <span>Hotline (Zalo): 0366 868 658</span>
        </div>
      </section>
      <section class="panel" aria-label="Đăng nhập"><ng-content /></section>
    </div>
  `,
  styleUrl: './auth-frame.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthFrameComponent {}
