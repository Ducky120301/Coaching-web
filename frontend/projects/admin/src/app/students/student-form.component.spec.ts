import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DRAFT_KEY, StudentFormComponent } from './student-form.component';
import { StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-25T02:00:00+00:00', registration: { weightKg: 72.5, sessionsPerWeek: '4 buổi' }, missingRequired: [],
    updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=', internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
    ...overrides,
  };
}

describe('StudentFormComponent', () => {
  beforeEach(() => localStorage.clear());

  function setup(mode: 'create' | 'edit', id?: string) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(StudentFormComponent);
    fixture.componentRef.setInput('mode', mode);
    if (id) fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement, navigate };
  }

  function fill(el: HTMLElement, key: string, value: string) {
    const control = el.querySelector(`#hs-${key}`) as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement;
    control.value = value;
    control.dispatchEvent(new Event(control instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  const submit = (el: HTMLElement) => el.querySelector('form')!.dispatchEvent(new Event('submit'));

  // @BR-f02-ho-so-hoc-vien-003 + NFR-f02-ho-so-hoc-vien-004: lưu thành công → xóa bản nháp, mở hồ sơ
  it('saves a profile with only name and email, clears the draft and opens the profile', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    expect(localStorage.getItem(DRAFT_KEY)).toContain('lan@gmail.com');

    submit(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.fullName).toBe('Lan');
    expect(request.request.body.email).toBe('lan@gmail.com');
    expect(request.request.body.registration['weightKg']).toBeNull();
    request.flush(detail());
    await settle();

    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // @BR-f02-ho-so-hoc-vien-014: ghi chú nội bộ gửi trong cùng POST tạo hồ sơ, không gọi endpoint ghi chú
  it('sends the internal note in the same create request', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'internalNote', ' Tránh lunge sâu vì gối phải ');

    submit(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.body.internalNote).toBe('Tránh lunge sâu vì gối phải');
    request.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải' }));
    await settle();

    http.expectNone('/api/admin/students/s1/internal-note');
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ nằm trên trình duyệt của coach
  it('restores the draft kept on this browser', () => {
    localStorage.setItem(DRAFT_KEY, JSON.stringify({ fullName: 'Quân', weightKg: '72,5' }));
    const { el } = setup('create');

    expect((el.querySelector('#hs-fullName') as HTMLInputElement).value).toBe('Quân');
    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect(el.textContent).toContain('2/30 mục bắt buộc');
  });

  it('"Xoá trắng hồ sơ" clears the form and the draft', () => {
    localStorage.setItem(DRAFT_KEY, JSON.stringify({ fullName: 'Quân' }));
    const { fixture, el } = setup('create');

    (el.querySelector('button.clear') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect((el.querySelector('#hs-fullName') as HTMLInputElement).value).toBe('');
  });

  // @BR-f02-ho-so-hoc-vien-008: "72,5" gửi 72.5; "3.500.000" gửi 3500000
  it('sends decimal-comma numbers as JSON numbers', () => {
    const { http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', '72,5');
    fill(el, 'price', '3.500.000');

    submit(el);

    const body = http.expectOne('/api/admin/students').request.body;
    expect(body.registration['weightKg']).toBe(72.5);
    expect(body.registration['price']).toBe(3500000);
  });

  // @BR-f02-ho-so-hoc-vien-008 (seed "abc"): ô số không hợp lệ → không gửi, đánh dấu ô
  it('does not send a non-numeric weight and marks the field', () => {
    const { fixture, http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', 'abc');

    submit(el);
    fixture.detectChanges();

    http.expectNone('/api/admin/students');
    expect(el.querySelector('#hs-weightKg')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.textContent).toContain('Nhập số hợp lệ');
  });

  // @BR-f02-ho-so-hoc-vien-009 (seed giá trị ngoài danh sách): select chỉ có lựa chọn cho trước
  it('offers only the allowed options for sessions per week', () => {
    const { el } = setup('create');
    const options = [...el.querySelectorAll('#hs-sessionsPerWeek option')].map((o) => o.textContent?.trim());
    expect(options).toEqual(['Chọn…', '2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi']);
  });

  // @BR-f02-ho-so-hoc-vien-001: server báo email đã dùng → hiện thông báo, giữ bản nháp
  it('shows the server message when the email is already used', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'an@mail.com');

    submit(el);
    http.expectOne('/api/admin/students').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(navigate).not.toHaveBeenCalled();
    expect(localStorage.getItem(DRAFT_KEY)).toContain('an@mail.com');
  });

  // @BR-f02-ho-so-hoc-vien-012 @BR-f02-ho-so-hoc-vien-015: sửa gửi version, email chỉ xem, 409 → tải lại
  it('edits with the version, keeps email read-only and offers reload on conflict', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(true);
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa hồ sơ');

    fill(el, 'weightKg', '70');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull(); // bản nháp chỉ ở chế độ tạo
    submit(el);
    const put = http.expectOne('/api/admin/students/s1');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.version).toBe('AAAAAAAAB9E=');
    expect(put.request.body.registration['weightKg']).toBe(70);
    expect(put.request.body.email).toBeUndefined();
    put.flush({ title: 'Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.', code: 'student.version_conflict' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    (el.querySelector('button.reload') as HTMLButtonElement).click();
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('button.reload')).toBeNull();
  });
});
