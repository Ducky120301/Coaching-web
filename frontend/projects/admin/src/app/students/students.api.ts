import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import { CoachingStatus, CreateStudentBody, ImportResult, StudentDetail, StudentListItem, UpdateStudentBody } from './student.models';

@Injectable({ providedIn: 'root' })
export class StudentsApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private url(path = ''): string {
    return this.auth.apiUrl(`/api/admin/students${path}`);
  }

  list(q: string): Observable<StudentListItem[]> {
    const query = q.trim();
    return this.http.get<StudentListItem[]>(this.url(), query ? { params: { q: query } } : {});
  }

  get(id: string): Observable<StudentDetail> {
    return this.http.get<StudentDetail>(this.url(`/${id}`));
  }

  create(body: CreateStudentBody): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(), body);
  }

  update(id: string, body: UpdateStudentBody): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}`), body);
  }

  updateNote(id: string, note: string, version: string): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}/internal-note`), { note, version });
  }

  changeStatus(id: string, status: CoachingStatus, reason: string, version: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/coaching-status`), { status, reason, version });
  }

  /** F01-P1: gửi / gửi lại thông tin đăng nhập → hồ sơ mới nhất (có `account`). */
  sendCredentials(id: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/credentials`), {});
  }

  /** F01-P1: sửa email khi tài khoản Chờ kích hoạt (đã gửi thông tin → server gửi thông tin mới tới email mới). */
  changeEmail(id: string, email: string, version: string): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}/email`), { email, version });
  }

  /** F01-P1: gửi link đặt lại mật khẩu cho tài khoản Đã kích hoạt. */
  sendResetLink(id: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/password-reset-link`), {});
  }

  /** F02 import Excel: multipart field `file` (Content-Type do trình duyệt đặt, kèm boundary). */
  importStudents(file: File): Observable<ImportResult> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<ImportResult>(this.url('/import'), body);
  }
}
