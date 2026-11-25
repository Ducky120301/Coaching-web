import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FORM_FIELDS } from './registration-fields';
import { DRAFT_KEY, EMAIL_CHANGE_NOTE, SAVED_NOT_SENT_MESSAGE, SENT_MESSAGE, StudentFormComponent } from './student-form.component';
import { AccountInfo, StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const notSent: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };
const sent: AccountInfo = {
  state: 'Sent', credentialsSentAt: '2026-11-25T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-28T02:00:00+00:00', activatedAt: null,
  history: [{ type: 'CredentialsSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }],
};

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-25T02:00:00+00:00', registration: { weightKg: 72.5, sessionsPerWeek: '4 buổi' }, missingRequired: [],
    updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=', internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
    account: notSent,
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

  /** Điền đủ 30 mục bắt buộc bằng giá trị hợp lệ. */
  function fillAllRequired(el: HTMLElement) {
    for (const f of FORM_FIELDS.filter((x) => x.required)) {
      const value = f.key === 'email' ? 'lan@gmail.com'
        : f.type === 'select' ? f.options![0]
        : f.type === 'date' ? '2026-11-01'
        : f.type === 'integer' ? '28'
        : f.type === 'decimal' ? '170'
        : f.key.startsWith('photo') ? 'https://drive.google.com/x' : 'Lan';
      fill(el, f.key, value);
    }
  }

  const submit = (el: HTMLElement) => el.querySelector('form')!.dispatchEvent(new Event('submit'));
  const saveOnly = (el: HTMLElement) => (el.querySelector('button.save-only') as HTMLButtonElement).click();

  it('offers the prototype actions on create', () => {
    const { el } = setup('create');
    const labels = [...el.querySelectorAll('.actions button')].map((b) => b.textContent?.trim());
    expect(labels).toEqual(['Lưu và gửi thông tin đăng nhập', 'Chỉ lưu hồ sơ', 'Xoá trắng hồ sơ']);
    expect(el.querySelector('.account')?.textContent).toContain('Chưa tạo');
    expect(el.querySelector('.account')?.textContent).toContain('Hồ sơ ở trạng thái Chờ kích hoạt sau khi gửi.');
    expect(el.querySelector('.receive h2')?.textContent?.trim()).toBe('Học viên sẽ nhận gì');
  });

  // @BR-f02-ho-so-hoc-vien-003 + NFR-f02-ho-so-hoc-vien-004: "Chỉ lưu hồ sơ" → xóa bản nháp, mở hồ sơ
  it('"Chỉ lưu hồ sơ" saves a profile with only name and email, clears the draft and opens the profile', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    expect(localStorage.getItem(DRAFT_KEY)).toContain('lan@gmail.com');

    saveOnly(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.fullName).toBe('Lan');
    expect(request.request.body.email).toBe('lan@gmail.com');
    expect(request.request.body.registration['weightKg']).toBeNull();
    request.flush(detail());
    await settle();

    http.expectNone('/api/admin/students/s1/credentials');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // @BR-f02-ho-so-hoc-vien-014: ghi chú nội bộ gửi trong cùng POST tạo hồ sơ
  it('sends the internal note in the same create request', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'internalNote', ' Tránh lunge sâu vì gối phải ');

    saveOnly(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.body.internalNote).toBe('Tránh lunge sâu vì gối phải');
    request.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải' }));
    await settle();

    http.expectNone('/api/admin/students/s1/internal-note');
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // NFR-f02-ho-so-hoc-vien-004
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

  // @BR-f02-ho-so-hoc-vien-008
  it('sends decimal-comma numbers as JSON numbers', () => {
    const { http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', '72,5');
    fill(el, 'price', '3.500.000');

    saveOnly(el);

    const body = http.expectOne('/api/admin/students').request.body;
    expect(body.registration['weightKg']).toBe(72.5);
    expect(body.registration['price']).toBe(3500000);
  });

  // @BR-f02-ho-so-hoc-vien-008 (seed "abc")
  it('does not send a non-numeric weight and marks the field', () => {
    const { fixture, http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', 'abc');

    saveOnly(el);
    fixture.detectChanges();

    http.expectNone('/api/admin/students');
    expect(el.querySelector('#hs-weightKg')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.textContent).toContain('Nhập số hợp lệ');
  });

  // @BR-f02-ho-so-hoc-vien-009
  it('offers only the allowed options for sessions per week', () => {
    const { el } = setup('create');
    const options = [...el.querySelectorAll('#hs-sessionsPerWeek option')].map((o) => o.textContent?.trim());
    expect(options).toEqual(['Chọn…', '2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi']);
  });

  // @BR-f02-ho-so-hoc-vien-001
  it('shows the server message when the email is already used', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'an@mail.com');

    saveOnly(el);
    http.expectOne('/api/admin/students').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(navigate).not.toHaveBeenCalled();
    expect(localStorage.getItem(DRAFT_KEY)).toContain('an@mail.com');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc → hồ sơ vẫn được lưu, không gửi, báo số mục thiếu
  it('"Lưu và gửi" with missing required fields saves the profile but does not send', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await new Promise((r) => setTimeout(r));

    http.expectNone((req) => req.url.endsWith('/credentials'));
    expect(navigate).toHaveBeenCalledWith(`/hoc-vien/${detail().id}`, {
      state: { notice: 'Đã lưu hồ sơ. Còn 28 mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.' },
    });
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-001: tạo rồi gửi thông tin đăng nhập; aside đổi sang "Đã gửi thông tin, chờ kích hoạt"
  it('"Lưu và gửi" creates the profile, sends the login details and shows the prototype message', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fillAllRequired(el);

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await settle();
    const post = http.expectOne('/api/admin/students/s1/credentials');
    expect(post.request.method).toBe('POST');
    post.flush(detail({ account: sent }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg.ok[role="status"]')?.textContent?.trim()).toBe(SENT_MESSAGE);
    expect(el.querySelector('.account')?.textContent).toContain('Đã gửi thông tin, chờ kích hoạt');
    expect(el.querySelector('.account')?.textContent).toContain('Gửi đến lan@gmail.com lúc 09:00. Mật khẩu tạm hết hạn sau 72 giờ.');
    expect((el.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('keeps the green message and writes no draft when editing after "Lưu và gửi" succeeded', async () => {
    const { fixture, http, el } = setup('create');
    fillAllRequired(el);
    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await settle();
    http.expectOne('/api/admin/students/s1/credentials').flush(detail({ account: sent }));
    await settle();
    fixture.detectChanges();

    fill(el, 'fullName', 'Lan Anh');
    fixture.detectChanges();

    expect(el.querySelector('p.msg.ok')?.textContent?.trim()).toBe(SENT_MESSAGE);
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ đã lưu nhưng gửi lỗi → sang trang hồ sơ kèm câu báo
  it('opens the profile with a notice when saving worked but sending failed', async () => {
    const { http, el, navigate } = setup('create');
    fillAllRequired(el);

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await settle();
    http.expectOne('/api/admin/students/s1/credentials').flush(
      { title: 'Chưa gửi được email. Vui lòng thử lại sau.', code: 'account.email_send_failed' },
      { status: 503, statusText: 'Service Unavailable' });
    await settle();

    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1', { state: { notice: SAVED_NOT_SENT_MESSAGE } });
  });

  // @BR-f02-ho-so-hoc-vien-012 @BR-f02-ho-so-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-007: đã kích hoạt → email chỉ xem
  it('edits with the version, keeps email read-only once activated and offers reload on conflict', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail({ accountStatus: 'Activated' }));
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(true);
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa hồ sơ');

    fill(el, 'weightKg', '70');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
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
    http.expectOne('/api/admin/students/s1').flush(detail({ accountStatus: 'Activated', version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('button.reload')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009: Chờ kích hoạt → sửa email qua PUT /email trước, rồi PUT hồ sơ
  it('changes the email of a pending account first, then saves the profile with the new version', async () => {
    const { fixture, http, el, navigate } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail({ email: 'lan@gmial.com', account: sent }));
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(false);
    expect(el.textContent).toContain(EMAIL_CHANGE_NOTE);

    fill(el, 'email', 'lan@gmail.com');
    submit(el);
    const email = http.expectOne('/api/admin/students/s1/email');
    expect(email.request.method).toBe('PUT');
    expect(email.request.body).toEqual({ email: 'lan@gmail.com', version: 'AAAAAAAAB9E=' });
    email.flush(detail({ email: 'lan@gmail.com', version: 'V2', account: sent }));
    await settle();
    const put = http.expectOne('/api/admin/students/s1');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.version).toBe('V2');
    put.flush(detail({ email: 'lan@gmail.com', version: 'V3', account: sent }));
    await settle();

    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  it('does not show the email-change note before any login details were sent, and skips PUT /email when unchanged', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    expect(el.textContent).not.toContain(EMAIL_CHANGE_NOTE);
    submit(el);
    http.expectNone('/api/admin/students/s1/email');
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-008: email đã dùng → câu của server, không phải banner xung đột phiên bản
  it('shows the server message, not the reload banner, when the new email is taken', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'email', 'an@mail.com');
    submit(el);
    http.expectOne('/api/admin/students/s1/email').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg')?.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(el.querySelector('button.reload')).toBeNull();
    http.expectNone('/api/admin/students/s1');
  });

  // @BR-f02-ho-so-hoc-vien-012
  it('sends the internal note with the version returned by the profile PUT', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'internalNote', 'Tránh lunge sâu');
    submit(el);
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
    const note = http.expectOne('/api/admin/students/s1/internal-note');
    expect(note.request.method).toBe('PUT');
    expect(note.request.body).toEqual({ note: 'Tránh lunge sâu', version: 'V2' });
    note.flush(detail({ version: 'V3', internalNote: 'Tránh lunge sâu' }));
    await settle();
  });

  it('retries with the fresh version when the note PUT failed after the profile PUT succeeded', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'internalNote', 'Tránh lunge sâu');
    submit(el);
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
    http.expectOne('/api/admin/students/s1/internal-note').flush({ title: 'Lỗi' }, { status: 500, statusText: 'Server Error' });
    await settle();
    fixture.detectChanges();

    submit(el);
    const retry = http.expectOne('/api/admin/students/s1');
    expect(retry.request.body.version).toBe('V2');
    retry.flush(detail({ version: 'V4' }));
    await settle();
    const note = http.expectOne('/api/admin/students/s1/internal-note');
    expect(note.request.body.version).toBe('V4');
    note.flush(detail({ version: 'V5', internalNote: 'Tránh lunge sâu' }));
    await settle();
  });

  it('disables submit and ignores submit in edit mode until the profile is loaded', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    const button = el.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    submit(el);
    await settle();
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
    http.verify();
  });
});
