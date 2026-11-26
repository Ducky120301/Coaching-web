import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { ExercisesApi, problemCode, problemTitle } from './exercises.api';

describe('ExercisesApi', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    return { api: TestBed.inject(ExercisesApi), http: TestBed.inject(HttpTestingController) };
  }

  it('lists without parameters by default and sends only the filters that are set', () => {
    const { api, http } = setup();
    api.list().subscribe();
    http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.keys().length === 0).flush([]);

    api.list({ q: '  fly ', muscleGroupId: 'g0', equipmentId: null, hidden: true }).subscribe();
    const req = http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.keys().length > 0);
    expect(req.request.params.get('q')).toBe('fly');
    expect(req.request.params.get('muscleGroupId')).toBe('g0');
    expect(req.request.params.has('equipmentId')).toBe(false);
    expect(req.request.params.get('hidden')).toBe('true');
    req.flush([]);
  });

  it('calls the contract routes for hide, unhide, delete and categories', () => {
    const { api, http } = setup();
    api.hide('x1').subscribe();
    expect(http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/hide' }).request.body).toEqual({});
    api.unhide('x1').subscribe();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/unhide' });
    api.remove('x1').subscribe();
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' });
    api.categories().subscribe();
    http.expectOne({ method: 'GET', url: '/api/admin/exercise-categories' });
    api.addCategory('Equipment', 'Resistance Band').subscribe();
    expect(http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' }).request.body)
      .toEqual({ kind: 'Equipment', name: 'Resistance Band' });
  });

  it('reads code and title from ProblemDetails, with a fallback title', () => {
    const problem = new HttpErrorResponse({ status: 409, error: { code: 'exercise.in_use', title: 'Bài đang có trong giáo án, chỉ ẩn được.' } });
    expect(problemCode(problem)).toBe('exercise.in_use');
    expect(problemTitle(problem, 'x')).toBe('Bài đang có trong giáo án, chỉ ẩn được.');
    expect(problemCode(new Error('boom'))).toBeNull();
    expect(problemTitle(new HttpErrorResponse({ status: 0 }), 'Thử lại.')).toBe('Thử lại.');
  });
});
