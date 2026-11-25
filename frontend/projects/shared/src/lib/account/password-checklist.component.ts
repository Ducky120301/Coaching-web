import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Checklist dưới 2 ô mật khẩu (page "Main", isActivate): tick màu --ok khi đạt. */
@Component({
  selector: 'dbx-password-checklist',
  template: `
    <ul>
      <li [class.met]="longEnough()">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"></path></svg>
        <span class="sr-only">{{ longEnough() ? 'Đã đạt: ' : 'Chưa đạt: ' }}</span>Ít nhất 8 ký tự
      </li>
      <li [class.met]="matches()">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"></path></svg>
        <span class="sr-only">{{ matches() ? 'Đã đạt: ' : 'Chưa đạt: ' }}</span>Hai lần nhập trùng nhau
      </li>
    </ul>
  `,
  styles: `
    :host { display: block; }
    ul { margin: 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 6px; font-size: 14px; color: var(--ink2); }
    li { display: flex; gap: 8px; align-items: center; }
    svg { color: var(--faint); flex: none; }
    li.met svg { color: var(--ok); }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordChecklistComponent {
  readonly password = input.required<string>();
  readonly confirm = input.required<string>();
  // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: đếm đúng từng ký tự (không trim), so khớp nguyên văn
  protected readonly longEnough = computed(() => this.password().length >= 8);
  protected readonly matches = computed(() => this.password().length > 0 && this.password() === this.confirm());
}
