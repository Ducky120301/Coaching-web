import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ICONS, StatusBadgeComponent, formatVnDate, initialsOf } from '@coaching/shared';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, merge, of, switchMap, tap } from 'rxjs';
import {
  ACCOUNT_STATUS_LABEL, ACCOUNT_STATUS_TONE, COACHING_STATUS_LABEL, COACHING_STATUS_TONE,
} from './student-status';
import { StudentListItem } from './student.models';
import { StudentsApi } from './students.api';

/**
 * Dashboard (danh sách tạm) — @ADR-f02-ho-so-hoc-vien-008: trang đích sau đăng nhập.
 * Style bảng/ô tìm/nút theo page Admin-Dashboard của prototype; chưa có bộ lọc con (F08).
 */
@Component({
  selector: 'app-student-list',
  imports: [RouterLink, StatusBadgeComponent],
  template: `
    <div class="head">
      <div>
        <p class="date">{{ today }}</p>
        <h1>Học viên</h1>
      </div>
      <a class="create" routerLink="/hoc-vien/moi">
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" [innerHTML]="plusIcon"></svg>Tạo hồ sơ học viên</a>
    </div>

    <div class="search">
      <svg aria-hidden="true" class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           stroke-width="2" stroke-linecap="round" [innerHTML]="searchIcon"></svg>
      <label for="q" class="sr-only">Tìm học viên</label>
      <input id="q" type="search" placeholder="Tìm theo tên hoặc email" (input)="onSearch($any($event.target).value)" />
    </div>

    <div class="table-wrap">
      @if (rows(); as list) {
        @if (list.length > 0) {
          <table>
            <thead>
              <tr>
                <th scope="col" class="first">Học viên</th>
                <th scope="col">Trạng thái coaching</th>
                <th scope="col" class="last">Tài khoản</th>
              </tr>
            </thead>
            <tbody>
              @for (s of list; track s.id) {
                <tr>
                  <td class="first">
                    <div class="who">
                      <span class="ini" aria-hidden="true">{{ initials(s.fullName) }}</span>
                      <span>
                        <a class="name" [routerLink]="['/hoc-vien', s.id]">{{ s.fullName }}</a>
                        <span class="email">{{ s.email }}</span>
                      </span>
                    </div>
                  </td>
                  <td><dbx-status-badge [tone]="coachingTone[s.coachingStatus]">{{ coachingLabel[s.coachingStatus] }}</dbx-status-badge></td>
                  <td class="last"><dbx-status-badge [tone]="accountTone[s.accountStatus]">{{ accountLabel[s.accountStatus] }}</dbx-status-badge></td>
                </tr>
              }
            </tbody>
          </table>
        } @else {
          <p class="empty">{{ query() ? 'Không tìm thấy học viên phù hợp.' : 'Chưa có hồ sơ học viên nào.' }}</p>
        }
      } @else if (failed()) {
        <p class="empty" role="alert">Không tải được danh sách học viên. Vui lòng tải lại trang.</p>
      }
    </div>
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 24px; }
    .head { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: flex-end; gap: 16px; }
    .date { margin: 0; font-size: 14px; color: var(--muted); }
    h1 { margin: 4px 0 0; font-family: var(--font-head); font-weight: 800; font-size: 48px; line-height: 1.05; }
    .create { height: 48px; padding: 0 22px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font-family: var(--font-head); font-weight: 700; font-size: 20px; text-decoration: none; display: inline-flex; align-items: center; gap: 8px; }
    .create:hover { color: var(--onPrimary); }
    .search { position: relative; }
    .search-icon { color: var(--muted); position: absolute; left: 14px; top: 15px; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
    input { width: 100%; box-sizing: border-box; height: 48px; border-radius: 14px; border: 1px solid var(--field);
      background: var(--surface); color: var(--ink); padding: 0 14px 0 42px; font: inherit; font-size: 15px; }
    .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 14px; background: var(--tableBg); }
    table { width: 100%; min-width: 880px; border-collapse: collapse; font-size: 14px; }
    thead tr { text-align: left; color: var(--muted); font-size: 13px; }
    th { padding: 14px 12px; font-weight: 500; border-bottom: 1px solid var(--line); }
    td { padding: 14px 12px; border-bottom: 1px solid var(--rowLine); }
    th.first, td.first { padding-left: 18px; }
    th.last, td.last { padding-right: 18px; }
    tbody tr:hover { background: var(--rowHover); }
    .who { display: flex; align-items: center; gap: 12px; }
    .ini { width: 36px; height: 36px; flex-shrink: 0; border-radius: 50%; background: var(--soft); color: var(--primary);
      display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 13px; }
    .name { display: block; font-weight: 600; color: var(--ink); text-decoration: none; }
    .name:hover { color: var(--linkHover); }
    .email { display: block; font-size: 12px; color: var(--muted); }
    .empty { margin: 0; padding: 32px; text-align: center; color: var(--muted); }
    @media (max-width: 879.98px) { h1 { font-size: 38px; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentListComponent {
  private readonly api = inject(StudentsApi);
  private readonly typed$ = new Subject<string>();
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly today = formatVnDate(new Date(), true);
  protected readonly plusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.plus);
  protected readonly searchIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.search);
  protected readonly coachingLabel = COACHING_STATUS_LABEL;
  protected readonly coachingTone = COACHING_STATUS_TONE;
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly accountTone = ACCOUNT_STATUS_TONE;
  protected readonly initials = initialsOf;

  protected readonly rows = signal<StudentListItem[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly query = signal('');

  constructor() {
    // Tải ngay lần đầu; khi gõ thì chờ 250ms ngừng gõ rồi mới tìm (từ khóa được server chuẩn hóa không dấu).
    merge(of(''), this.typed$.pipe(debounceTime(250)))
      .pipe(
        map((q) => q.trim()),
        distinctUntilChanged(),
        tap((q) => this.query.set(q)),
        switchMap((q) => this.api.list(q).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((rows) => {
        this.rows.set(rows);
        this.failed.set(rows === null);
      });
  }

  protected onSearch(value: string): void {
    this.typed$.next(value);
  }
}
