import { Routes } from '@angular/router';
import { LoginPageComponent, authGuard, guestGuard } from '@coaching/shared';
import { ClientHomeComponent } from './home/client-home.component';
import { ClientShellComponent } from './shell/client-shell.component';

export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Duobexiuuu Coaching' },
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
