import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type BadgeTone = 'ok' | 'warn' | 'paused' | 'ended';

/** Chip trạng thái — style lấy từ prototype (Admin-Dashboard statusStyle; AdHoSo header = dense). */
@Component({
  selector: 'dbx-status-badge',
  template: `<span class="badge" [class]="tone()" [class.dense]="dense()"><ng-content /></span>`,
  styles: `
    .badge { display: inline-block; border-radius: 999px; padding: 4px 10px; font-size: 12px; font-weight: 600; white-space: nowrap; }
    .dense { padding: 3px 10px; }
    .ok { background: var(--okBg); color: var(--ok); }
    .ok.dense { color: var(--okInk); }
    .warn { background: var(--warnBg); color: var(--warn); }
    .paused { background: var(--pausedBg); color: var(--ink2); }
    .ended { background: var(--pausedBg); color: var(--muted); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly tone = input.required<BadgeTone>();
  readonly dense = input(false);
}
