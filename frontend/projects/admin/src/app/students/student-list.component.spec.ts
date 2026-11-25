import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { StudentListComponent } from './student-list.component';
import { StudentListItem } from './student.models';

const ROWS: StudentListItem[] = [
  { id: 'b', fullName: 'Phạm Hoàng Quân', email: 'hquanbilly@gmail.com', coachingStatus: 'Active', accountStatus: 'PendingActivation', createdAt: '2026-11-25T02:00:00+00:00' },
  { id: 'a', fullName: 'Nam', email: 'nam@gmail.com', coachingStatus: 'Ended', accountStatus: 'Activated', createdAt: '2026-11-24T02:00:00+00:00' },
];

describe('StudentListComponent', () => {
  afterEach(() => vi.useRealTimers());

  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(StudentListComponent);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement };
  }

  function type(el: HTMLElement, text: string) {
    const input = el.querySelector('input[type=search]') as HTMLInputElement;
    input.value = text;
    input.dispatchEvent(new Event('input'));
  }

  // @ADR-f02-ho-so-hoc-vien-008
  it('lists every student in server order with Vietnamese statuses and profile links', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);
    fixture.detectChanges();

    const rows = [...el.querySelectorAll('tbody tr')].map((tr) => tr.textContent ?? '');
    expect(rows).toHaveLength(2);
    expect(rows[0]).toContain('Phạm Hoàng Quân');
    expect(rows[0]).toContain('hquanbilly@gmail.com');
    expect(rows[0]).toContain('Đang coaching');
    expect(rows[0]).toContain('Chờ kích hoạt');
    expect(rows[1]).toContain('Đã kết thúc');
    expect(rows[1]).toContain('Đã kích hoạt');
    expect(el.querySelector('tbody a')?.getAttribute('href')).toBe('/hoc-vien/b');
    expect(el.querySelector('a.create')?.getAttribute('href')).toBe('/hoc-vien/moi');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Học viên');
  });

  it('searches with the typed keyword after typing pauses', () => {
    vi.useFakeTimers();
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);

    type(el, 'Quân');
    vi.advanceTimersByTime(250);
    http.expectOne((r) => r.url === '/api/admin/students' && r.params.get('q') === 'Quân').flush([ROWS[0]]);
    fixture.detectChanges();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('shows the empty search state', () => {
    vi.useFakeTimers();
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);

    type(el, 'zzz');
    vi.advanceTimersByTime(250);
    http.expectOne((r) => r.params.get('q') === 'zzz').flush([]);
    fixture.detectChanges();

    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('Không tìm thấy học viên phù hợp.');
  });

  it('shows an error line when the list cannot be loaded', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Không tải được danh sách học viên. Vui lòng tải lại trang.');
  });
});
