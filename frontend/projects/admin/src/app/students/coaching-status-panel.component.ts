import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

/**
 * Khung lý do đổi trạng thái coaching — mở ngay dưới header (tech-design: suy ra, PO duyệt 2026-11-25).
 * @BR-f02-ho-so-hoc-vien-018: lý do bắt buộc, tối đa 500 ký tự (server kiểm lại).
 */
@Component({
  selector: 'app-coaching-status-panel',
  template: `
    <section class="panel" aria-labelledby="status-panel-title">
      <h2 id="status-panel-title">{{ action() }}</h2>
      <label for="status-reason">Lý do</label>
      <textarea id="status-reason" rows="3" maxlength="500" [value]="reason()"
                (input)="reason.set($any($event.target).value)"></textarea>
      @if (localError() ?? error(); as message) {
        <p class="msg" role="status">{{ message }}</p>
      }
      <div class="actions">
        <button type="button" class="primary" [disabled]="busy()" (click)="submit()">Xác nhận</button>
        <button type="button" class="secondary" (click)="cancelled.emit()">Huỷ</button>
      </div>
    </section>
  `,
  styles: `
    .panel { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex; flex-direction: column; gap: 10px; }
    h2 { margin: 0; font-size: 16px; font-weight: 600; }
    label { font-size: 14px; font-weight: 600; line-height: 1.35; }
    textarea { box-sizing: border-box; width: 100%; border-radius: 14px; background: var(--surface); color: var(--ink); font: inherit;
      font-size: 15px; border: 1px solid var(--field); padding: 12px 14px; line-height: 1.5; resize: vertical; }
    .msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .primary { height: 44px; padding: 0 20px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
    .primary:disabled { opacity: .6; cursor: default; }
    .secondary { height: 44px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
      color: var(--ink); font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingStatusPanelComponent {
  /** Tên hành động, vd. "Tạm dừng". */
  readonly action = input.required<string>();
  /** Lỗi từ server (ProblemDetails.title). */
  readonly error = input<string | null>(null);
  readonly busy = input(false);
  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  protected readonly reason = signal('');
  protected readonly localError = signal<string | null>(null);

  protected submit(): void {
    const text = this.reason().trim();
    if (!text) {
      this.localError.set('Cần nhập lý do đổi trạng thái coaching.');
      return;
    }
    this.localError.set(null);
    this.confirmed.emit(text);
  }
}
