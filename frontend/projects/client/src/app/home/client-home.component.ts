import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { AuthService, formatVnDate } from '@coaching/shared';

@Component({
  selector: 'app-client-home',
  template: `
    <header>
      <p class="date">{{ today }}</p>
      <h1>Chào {{ givenName() }}</h1>
    </header>
    <p class="note">Giáo án sẽ hiển thị ở đây khi coach phát hành.</p>
  `,
  styles: `
    .date { margin: 0; font-size: 14px; color: var(--muted); }
    h1 { margin: 0; font-family: var(--font-head); font-weight: 800; line-height: 1; letter-spacing: -.01em; font-size: 38px; }
    @media (min-width: 880px) { h1 { font-size: 54px; } }
    .note { margin: 0; font-size: 15px; color: var(--ink2); line-height: 1.6; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientHomeComponent {
  private readonly auth = inject(AuthService);
  protected readonly today = formatVnDate(new Date(), false);
  protected readonly givenName = computed(() => (this.auth.user()?.fullName ?? '').trim().split(/\s+/).at(-1) ?? '');
}
