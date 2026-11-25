import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import { StudentListItem } from './student.models';

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
}
