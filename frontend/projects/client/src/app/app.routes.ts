import { Routes } from '@angular/router';
import {
  ActivatePageComponent, ForgotPasswordPageComponent, LoginPageComponent, ResetPasswordPageComponent,
  activationGuard, authGuard, guestGuard,
} from '@coaching/shared';
import { ClientHomeComponent } from './home/client-home.component';
import { ClientShellComponent } from './shell/client-shell.component';

// @ADR-f01-p1-tai-khoan-hoc-vien-010: route tiếng Việt; mã kích hoạt chỉ trong bộ nhớ, token đặt lại nằm sau '#t='.
// /dat-lai-mat-khau không dùng guestGuard: học viên đang đăng nhập trên máy này vẫn đặt lại được (phiên bị xoá sau khi lưu).
export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Duobexiuuu Coaching' },
  { path: 'kich-hoat', component: ActivatePageComponent, canActivate: [guestGuard, activationGuard], title: 'Kích hoạt tài khoản — Duobexiuuu Coaching' },
  { path: 'quen-mat-khau', component: ForgotPasswordPageComponent, canActivate: [guestGuard], title: 'Quên mật khẩu — Duobexiuuu Coaching' },
  { path: 'dat-lai-mat-khau', component: ResetPasswordPageComponent, title: 'Đặt lại mật khẩu — Duobexiuuu Coaching' },
  {
    path: '',
    component: ClientShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'hom-nay' },
      { path: 'hom-nay', component: ClientHomeComponent, title: 'Hôm nay — Duobexiuuu Coaching' },
    ],
  },
  { path: '**', redirectTo: '' },
];
