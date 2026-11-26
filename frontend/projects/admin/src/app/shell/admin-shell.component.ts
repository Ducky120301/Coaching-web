import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, ICONS, UserMenuComponent } from '@coaching/shared';
import { catchError, map, of } from 'rxjs';

interface CoachProfile { id: string; displayName: string; email: string; }

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UserMenuComponent],
  template: `
    <div class="root">
      <aside class="rail">
        <div class="brand">
          <p class="sig">duobexiuuu</p>
          <p class="area muted">Khu vực coach</p>
        </div>
        <nav aria-label="Admin">
          <a routerLink="/dashboard" routerLinkActive ariaCurrentWhenActive="page">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="homeIcon"></svg>Dashboard</a>
          <a routerLink="/hoc-vien/moi" routerLinkActive ariaCurrentWhenActive="page">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="userPlusIcon"></svg>Tạo hồ sơ học viên</a>
          <a routerLink="/thu-vien/bai-tap" routerLinkActive ariaCurrentWhenActive="page">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="dumbbellIcon"></svg>Thư viện bài tập</a>
        </nav>
        <div class="user-block">
          <dbx-user-menu [name]="coachName() ?? auth.user()?.fullName ?? ''" roleLabel="Quản trị viên" />
        </div>
      </aside>
      <main><router-outlet /></main>
    </div>
  `,
  styles: `
    .root { min-height: 100vh; box-sizing: border-box; background: var(--bg); color: var(--ink); font-family: var(--font-body); display: flex; flex-wrap: wrap; }
    .rail { flex: 1 1 220px; max-width: 260px; min-width: 220px; box-sizing: border-box; background: var(--chrome);
      border-right: 1px solid var(--line); padding: 24px 16px; display: flex; flex-direction: column; gap: 28px; }
    .brand { padding: 0 8px; }
    .sig { margin: 0; font-family: var(--font-sig); font-size: 34px; color: var(--sig); line-height: 1; }
    .area { margin: 4px 0 0; font-size: 13px; color: var(--muted); }
    nav { display: flex; flex-direction: column; gap: 2px; }
    nav a { text-decoration: none; display: flex; align-items: center; gap: 10px; padding: 11px 12px; border-radius: 12px; font-size: 15px; font-weight: 600; }
    .user-block { margin-top: auto; }
    /* Khung thôi: padding + gap do từng trang đặt theo đúng page trong prototype (router-outlet là phần tử con, không được là flex item có gap). */
    main { flex: 999 1 560px; min-width: 0; box-sizing: border-box; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private readonly sanitizer = inject(DomSanitizer);
  protected readonly auth = inject(AuthService);
  protected readonly homeIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.home);
  protected readonly userPlusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.userPlus);
  protected readonly dumbbellIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.dumbbell);
  /** Tên coach lấy từ hệ thống (hồ sơ coach). */
  protected readonly coachName = toSignal(
    inject(HttpClient).get<CoachProfile>(this.auth.apiUrl('/api/admin/profile')).pipe(
      map((p) => p.displayName),
      catchError(() => of(null)),
    ),
    { initialValue: null },
  );
}
