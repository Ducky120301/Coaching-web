import { Routes } from '@angular/router';
import { LoginPageComponent, authGuard, guestGuard } from '@coaching/shared';
import { AdminHomeComponent } from './home/admin-home.component';
import { AdminShellComponent } from './shell/admin-shell.component';

export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Khu vực coach' },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },
      { path: 'trang-chu', component: AdminHomeComponent, title: 'Trang chủ — Khu vực coach' },
    ],
  },
  { path: '**', redirectTo: '' },
];
