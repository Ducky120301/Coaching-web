import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, ICONS, UserMenuComponent } from '@coaching/shared';

@Component({
  selector: 'app-client-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UserMenuComponent],
  template: `
    <div class="root">
      <aside class="rail">
        <p class="sig">duobexiuuu</p>
        <nav aria-label="Điều hướng chính">
          <a routerLink="/hom-nay" routerLinkActive #r1="routerLinkActive" [attr.aria-current]="r1.isActive ? 'page' : null">
            <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="todayIcon"></svg>Hôm nay</a>
        </nav>
        <div class="user-block">
          <dbx-user-menu [name]="auth.user()?.fullName ?? ''" roleLabel="Học viên" />
        </div>
      </aside>
      <main><router-outlet /></main>
      <nav class="tabbar" aria-label="Điều hướng chính">
        <a routerLink="/hom-nay" routerLinkActive #r2="routerLinkActive" [attr.aria-current]="r2.isActive ? 'page' : null">
          <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round" [innerHTML]="todayIcon"></svg>Hôm nay</a>
        <dbx-user-menu class="tab-user" [name]="auth.user()?.fullName ?? ''" roleLabel="Học viên" variant="tab" />
      </nav>
    </div>
  `,
  styles: `
    .root { min-height: 100vh; box-sizing: border-box; background: var(--bg); color: var(--ink); font-family: var(--font-body);
      display: flex; flex-direction: column; }
    .rail { display: none; }
    main { flex: 1; min-width: 0; width: 100%; max-width: 1160px; box-sizing: border-box; display: flex; flex-direction: column;
      gap: 20px; padding: 16px 20px 24px; }
    .tabbar { position: sticky; bottom: 0; margin-top: auto; border-top: 1px solid var(--line); background: var(--chrome);
      display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); padding: 6px 4px 14px; z-index: 2; }
    .tabbar a { text-decoration: none; display: flex; flex-direction: column; align-items: center; gap: 4px; padding: 8px 0;
      font-size: 12px; color: var(--muted); }
    .tabbar a[aria-current='page'] { color: var(--primary); font-weight: 600; }
    @media (min-width: 880px) {
      .root { flex-direction: row; }
      .rail { flex: 0 0 240px; box-sizing: border-box; background: var(--chrome); border-right: 1px solid var(--line);
        padding: 28px 16px; display: flex; flex-direction: column; gap: 28px; }
      .tabbar { display: none; }
      main { padding: 40px 48px 56px; }
    }
    .sig { margin: 0 8px; font-family: var(--font-sig); font-size: 34px; color: var(--sig); line-height: 1; }
    .rail nav { display: flex; flex-direction: column; gap: 4px; }
    .rail nav a { text-decoration: none; display: flex; align-items: center; gap: 12px; padding: 12px; border-radius: 12px; font-size: 15px; }
    .user-block { margin-top: auto; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly todayIcon = inject(DomSanitizer).bypassSecurityTrustHtml(ICONS.today);
}
