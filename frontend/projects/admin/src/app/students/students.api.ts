import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import { CreateStudentBody, StudentDetail, StudentListItem, UpdateStudentBody } from './student.models';

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
}
