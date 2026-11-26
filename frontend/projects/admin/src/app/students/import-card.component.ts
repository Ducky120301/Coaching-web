import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ImportResult } from './student.models';
import { StudentsApi } from './students.api';

/** Câu nguyên văn tech-design "Thiết kế giao diện (UI)". */
export const IMPORT_HINT = 'Tải file .xlsx câu trả lời Google Form (tối đa 5 MB, 500 dòng). Hồ sơ được tạo ở Chờ kích hoạt, chưa gửi email.';
/** 413 của Kestrel/nginx không có ProblemDetails → cùng câu với `import.file_too_large` (@BR-f02-import-excel-015). */
export const IMPORT_TOO_LARGE = 'File vượt giới hạn 5 MB.';
export const IMPORT_FAILED = 'Không import được file. Vui lòng thử lại.';

interface ErrorLine {
  row: number;
  email: string;
  column: string;
  reason: string;
}

/**
 * Card "Import từ Excel" ở đầu trang Tạo hồ sơ (tech-design UI, PO chốt 2026-11-26).
 * Kết quả theo @BR-f02-import-excel-034: "Đã tạo {x}, bỏ qua {y}" + mỗi ô sai một dòng bảng (Dòng, Email, Cột, Lý do).
 */
@Component({
  selector: 'app-import-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="import" aria-labelledby="import-title">
      <h2 id="import-title">Import từ Excel</h2>
      <p class="hint">{{ hint }}</p>
      <div class="row">
        <input id="import-file" class="sr-only" type="file"
               accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" (change)="pick($event)" />
        <label class="choose" for="import-file">Chọn file .xlsx</label>
        <span class="file">{{ file()?.name ?? 'Chưa chọn file' }}</span>
        <button type="button" class="primary" [disabled]="!file() || busy()" (click)="run()">Import</button>
      </div>
      @if (fileError(); as e) {
        <p class="box warn" role="status">{{ e }}</p>
      }
      @if (result(); as r) {
        <p class="box" [class.warn]="r.skipped > 0" role="status">Đã tạo {{ r.created }}, bỏ qua {{ r.skipped }}</p>
        @if (errorLines().length > 0) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col" class="first">Dòng</th>
                  <th scope="col">Email</th>
                  <th scope="col">Cột</th>
                  <th scope="col" class="last">Lý do</th>
                </tr>
              </thead>
              <tbody>
                @for (line of errorLines(); track $index) {
                  <tr>
                    <td class="first num">{{ line.row }}</td>
                    <td>{{ line.email }}</td>
                    <td>{{ line.column }}</td>
                    <td class="last">{{ line.reason }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </section>
  `,
  styles: `
    :host { display: block; }
    .import { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px;
      display: flex; flex-direction: column; gap: 10px; }
    h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 22px; }
    .hint { margin: 0; font-size: 14px; line-height: 1.5; color: var(--muted); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
    .choose { box-sizing: border-box; height: 44px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field);
      background: var(--surface); color: var(--ink); font-weight: 600; font-size: 14px; display: inline-flex; align-items: center;
      cursor: pointer; }
    .sr-only:focus-visible + .choose { outline: 2px solid var(--primary); outline-offset: 2px; }
    .file { min-width: 0; font-size: 14px; color: var(--ink2); overflow-wrap: anywhere; }
    .primary { height: 44px; padding: 0 22px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font: inherit; font-weight: 600; font-size: 14px; cursor: pointer; }
    .primary:disabled { opacity: .6; cursor: default; }
    .box { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--okBg); color: var(--okInk); }
    .box.warn { background: var(--warnBg); color: var(--warn); }
    .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 14px; background: var(--tableBg); }
    table { width: 100%; min-width: 560px; border-collapse: collapse; font-size: 14px; }
    thead tr { text-align: left; color: var(--muted); font-size: 13px; }
    th { padding: 14px 12px; font-weight: 500; border-bottom: 1px solid var(--line); }
    td { padding: 14px 12px; border-bottom: 1px solid var(--rowLine); vertical-align: top; }
    tbody tr:last-child td { border-bottom: 0; }
    th.first, td.first { padding-left: 18px; }
    th.last, td.last { padding-right: 18px; }
    .num { font-variant-numeric: tabular-nums; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
})
export class ImportCardComponent {
  private readonly api = inject(StudentsApi);

  protected readonly hint = IMPORT_HINT;
  protected readonly file = signal<File | null>(null);
  protected readonly busy = signal(false);
  protected readonly result = signal<ImportResult | null>(null);
  protected readonly fileError = signal<string | null>(null);
  /** Mỗi ô sai của dòng bị bỏ qua là một dòng bảng; dòng đã tạo không hiện. */
  protected readonly errorLines = computed<ErrorLine[]>(() =>
    (this.result()?.rows ?? [])
      .filter((r) => r.status === 'Skipped')
      .flatMap((r) => r.errors.map((e) => ({ row: r.row, email: r.email ?? '—', column: e.column, reason: e.reason }))));

  protected pick(event: Event): void {
    this.file.set((event.target as HTMLInputElement).files?.[0] ?? null);
    this.result.set(null);
    this.fileError.set(null);
  }

  protected async run(): Promise<void> {
    const file = this.file();
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.result.set(null);
    this.fileError.set(null);
    try {
      this.result.set(await firstValueFrom(this.api.importStudents(file)));
    } catch (error) {
      this.fileError.set(fileErrorText(error));
    } finally {
      this.busy.set(false);
    }
  }
}

/** Lỗi cả file (@BR-f02-import-excel-014/015/016/019/021): `title` của ProblemDetails; 413 không có body JSON. */
function fileErrorText(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 413) return IMPORT_TOO_LARGE;
    if (typeof error.error?.title === 'string') return error.error.title;
  }
  return IMPORT_FAILED;
}
