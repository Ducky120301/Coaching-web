import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import { AuthService } from '../auth/auth.service';

/** Khối avatar + tên + vai trò; bấm vào mở menu "Đăng xuất" (quyết định UX của PO). */
@Component({
  selector: 'dbx-user-menu',
  template: `
    <div class="wrap" [class.tab]="variant() === 'tab'">
      <button #trigger type="button" class="user-block" [attr.aria-expanded]="open()" aria-haspopup="menu" (click)="open.set(!open())">
        <span class="avatar" aria-hidden="true">{{ initials() }}</span>
        @if (variant() === 'rail') {
          <span class="who"><span class="name">{{ name() }}</span><span class="role muted">{{ roleLabel() }}</span></span>
        } @else {
          <span class="tab-label">Tài khoản</span>
        }
      </button>
      @if (open()) {
        <div class="menu" role="menu">
          <button type="button" role="menuitem" (click)="logout()">Đăng xuất</button>
        </div>
      }
    </div>
  `,
  host: {
    '(document:keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
  styles: `
    :host { display: block; }
    .wrap { position: relative; }
    .user-block { display: flex; align-items: center; gap: 10px; padding: 8px; width: 100%; border: 0; background: none;
      color: inherit; font: inherit; text-align: left; cursor: pointer; border-radius: 12px; }
    .avatar { width: 36px; height: 36px; border-radius: 50%; background: var(--primary); color: var(--onPrimary);
      display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; flex: none; }
    .who { display: flex; flex-direction: column; }
    .name { font-size: 14px; font-weight: 600; }
    .role { font-size: 12px; color: var(--muted); }
    .menu { position: absolute; left: 8px; bottom: calc(100% + 4px); min-width: 160px; background: var(--surface);
      border-radius: 12px; box-shadow: inset 0 0 0 1px var(--line); padding: 6px; z-index: 5; }
    .menu button { display: block; width: 100%; height: 40px; padding: 0 12px; border: 0; border-radius: 10px;
      background: none; color: var(--ink); font: inherit; font-size: 14px; text-align: left; cursor: pointer; }
    .menu button:hover { background: var(--rowHover); }
    .tab .user-block { flex-direction: column; gap: 4px; padding: 8px 0; justify-content: center; font-size: 12px; color: var(--muted); }
    .tab .avatar { width: 22px; height: 22px; font-size: 10px; }
    .tab .menu { left: auto; right: 4px; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserMenuComponent {
  private readonly auth = inject(AuthService);
  readonly name = input.required<string>();
  readonly roleLabel = input.required<string>();
  readonly variant = input<'rail' | 'tab'>('rail');
  readonly open = signal(false);
  readonly initials = computed(() =>
    this.name().trim().split(/\s+/).filter(Boolean).map((w) => w[0]).slice(-2).join('').toUpperCase() || '?');

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly trigger = viewChild<ElementRef<HTMLButtonElement>>('trigger');

  onEscape(): void {
    if (!this.open()) return;
    this.open.set(false);
    this.trigger()?.nativeElement.focus();
  }

  onDocumentClick(event: Event): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }

  logout(): void {
    this.open.set(false);
    void this.auth.logout();
  }
}
