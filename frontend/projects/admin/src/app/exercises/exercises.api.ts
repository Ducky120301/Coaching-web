import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import {
  Categories, CategoryCreated, CategoryKind, ExerciseDetail, ExerciseFilter, ExerciseListItem, SaveExerciseBody,
} from './exercise.models';

@Injectable({ providedIn: 'root' })
export class ExercisesApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private url(path = ''): string {
    return this.auth.apiUrl(`/api/admin/exercises${path}`);
  }

  /** Tìm không dấu ở server (@BR-f03-thu-vien-bai-tap-018); lọc kết hợp (@BR-f03-thu-vien-bai-tap-019). */
  list(filter: ExerciseFilter = {}): Observable<ExerciseListItem[]> {
    let params = new HttpParams();
    const q = filter.q?.trim();
    if (q) params = params.set('q', q);
    if (filter.muscleGroupId) params = params.set('muscleGroupId', filter.muscleGroupId);
    if (filter.equipmentId) params = params.set('equipmentId', filter.equipmentId);
    if (filter.hidden) params = params.set('hidden', 'true');
    return this.http.get<ExerciseListItem[]>(this.url(), { params });
  }

  get(id: string): Observable<ExerciseDetail> {
    return this.http.get<ExerciseDetail>(this.url(`/${id}`));
  }

  create(body: SaveExerciseBody): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(), body);
  }

  update(id: string, body: SaveExerciseBody): Observable<ExerciseDetail> {
    return this.http.put<ExerciseDetail>(this.url(`/${id}`), body);
  }

  hide(id: string): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(`/${id}/hide`), {});
  }

  unhide(id: string): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(`/${id}/unhide`), {});
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(this.url(`/${id}`));
  }

  categories(): Observable<Categories> {
    return this.http.get<Categories>(this.auth.apiUrl('/api/admin/exercise-categories'));
  }

  addCategory(kind: CategoryKind, name: string): Observable<CategoryCreated> {
    return this.http.post<CategoryCreated>(this.auth.apiUrl('/api/admin/exercise-categories'), { kind, name });
  }
}

/** `code` của ProblemDetails (ResultExtensions.ToProblem), null khi không phải lỗi API. */
export function problemCode(error: unknown): string | null {
  return error instanceof HttpErrorResponse && typeof error.error?.code === 'string' ? error.error.code : null;
}

/** `title` tiếng Việt của ProblemDetails, hoặc câu dự phòng (mất mạng, 5xx không body). */
export function problemTitle(error: unknown, fallback: string): string {
  return error instanceof HttpErrorResponse && typeof error.error?.title === 'string' ? error.error.title : fallback;
}
