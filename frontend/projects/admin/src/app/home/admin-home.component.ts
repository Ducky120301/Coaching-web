import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { AuthService, formatVnDate } from '@coaching/shared';

interface CoachProfile { id: string; displayName: string; email: string; }

@Component({
  selector: 'app-admin-home',
  template: `
    <header>
      <p class="date">{{ today }}</p>
      @if (profile(); as p) {
        <h1>Xin chào, {{ p.displayName }}</h1>
        <p class="email">{{ p.email }}</p>
      }
      @if (profile() === null) {
        <p class="error">Không tải được thông tin coach. Vui lòng tải lại trang.</p>
      }
    </header>
  `,
  styles: `
    .date { margin: 0; font-size: 14px; color: var(--muted); }
    h1 { margin: 4px 0 0; font-family: var(--font-head); font-weight: 800; font-size: 48px; line-height: 1.05; }
    .error { margin: 8px 0 0; font-size: 14px; color: var(--muted); }
    .email { margin: 8px 0 0; font-size: 15px; color: var(--ink2); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminHomeComponent {
  private readonly auth = inject(AuthService);
  protected readonly today = formatVnDate(new Date(), true);
  protected readonly profile = toSignal(
    inject(HttpClient).get<CoachProfile>(this.auth.apiUrl('/api/admin/profile')).pipe(catchError(() => of(null))),
    { initialValue: undefined },
  );
}
