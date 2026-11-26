import { Routes } from '@angular/router';
import { LoginPageComponent, authGuard, guestGuard } from '@coaching/shared';
import { AdminShellComponent } from './shell/admin-shell.component';

// @ADR-f02-ho-so-hoc-vien-008: route tiếng Việt; dashboard (danh sách tạm) là trang đích thay trang-chu
export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Khu vực coach' },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'trang-chu', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./students/student-list.component').then((m) => m.StudentListComponent),
        title: 'Học viên — Khu vực coach',
      },
      {
        path: 'hoc-vien/moi',
        loadComponent: () => import('./students/student-form.component').then((m) => m.StudentFormComponent),
        data: { mode: 'create' },
        title: 'Tạo hồ sơ học viên — Khu vực coach',
      },
      {
        path: 'hoc-vien/:id',
        loadComponent: () => import('./students/student-profile.component').then((m) => m.StudentProfileComponent),
        title: 'Hồ sơ học viên — Khu vực coach',
      },
      {
        path: 'hoc-vien/:id/sua',
        loadComponent: () => import('./students/student-form.component').then((m) => m.StudentFormComponent),
        data: { mode: 'edit' },
        title: 'Sửa hồ sơ — Khu vực coach',
      },
      // F03: route tiếng Việt theo @ADR-f02-ho-so-hoc-vien-008
      {
        path: 'thu-vien/bai-tap',
        loadComponent: () => import('./exercises/exercise-library.component').then((m) => m.ExerciseLibraryComponent),
        title: 'Thư viện bài tập — Khu vực coach',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
