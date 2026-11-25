import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { StudentProfileComponent } from './student-profile.component';
import { AccountInfo, StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const notSent: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };
const sent: AccountInfo = {
  state: 'Sent', credentialsSentAt: '2026-11-25T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-28T02:00:00+00:00', activatedAt: null,
  history: [{ type: 'CredentialsSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }],
};
const activated: AccountInfo = {
  state: 'Activated', credentialsSentAt: '2026-10-12T02:05:00+00:00', tempPasswordExpiresAt: null, activatedAt: '2026-10-13T01:12:00+00:00',
  history: [
    { type: 'Activated', at: '2026-10-13T01:12:00+00:00', fromEmail: null, toEmail: null },
    { type: 'EmailChanged', at: '2026-10-12T14:30:00+00:00', fromEmail: 'lan@gmial.com', toEmail: 'lan@gmail.com' },
    { type: 'CredentialsSent', at: '2026-10-12T02:05:00+00:00', fromEmail: null, toEmail: null },
  ],
};

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-20T02:00:00+00:00',
    registration: { trainingGoal: 'Giảm mỡ, giữ cơ', availability: 'T3–T7 buổi tối', equipment: null, weightKg: 72.5 },
    missingRequired: ['pack', 'photoBackUrl'], updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=',
    internalNote: null, internalNoteUpdatedAt: null, statusHistory: [], account: notSent,
    ...overrides,
  };
}

describe('StudentProfileComponent', () => {
  async function setup(initial: StudentDetail = detail()) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(StudentProfileComponent);
    fixture.componentRef.setInput('id', 's1');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/admin/students/s1').flush(initial);
    await settle();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const buttons = () => [...el.querySelectorAll('button')].map((b) => b.textContent?.trim());
    const click = (text: string) => ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement).click();
    return { fixture, http, el, buttons, click };
  }

  it('shows the header, statuses, actions of an active student and missing required fields', async () => {
    const { el, buttons } = await setup();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Lan');
    expect(el.querySelector('.meta')?.textContent).toContain('lan@gmail.com');
    expect(el.querySelector('.meta')?.textContent).toContain('Đang coaching');
    expect(el.querySelector('.meta')?.textContent).toContain('Chờ kích hoạt');
    expect(buttons()).toContain('Tạm dừng');
    expect(buttons()).toContain('Kết thúc');
    expect(buttons()).not.toContain('Tiếp tục coaching');
    expect(el.textContent).toContain('Giảm mỡ, giữ cơ');
    expect(el.querySelector('.missing')?.textContent).toContain('Gói đã mua');
    expect(el.querySelector('.missing')?.textContent).toContain('Ảnh mặt sau');
    expect(el.querySelector('a.edit')?.getAttribute('href')).toBe('/hoc-vien/s1/sua');
    expect(el.textContent).toContain('Chưa đổi trạng thái lần nào.');
  });

  // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020: đổi trạng thái có lý do, thêm một dòng lịch sử
  it('changes the coaching status with a reason and shows the new history row', async () => {
    const { fixture, http, el, buttons, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');

    const post = http.expectOne('/api/admin/students/s1/coaching-status');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ status: 'Paused', reason: 'Đi công tác', version: 'AAAAAAAAB9E=' });
    post.flush(detail({
      coachingStatus: 'Paused', version: 'AAAAAAAAB9I=',
      statusHistory: [{ from: 'Active', to: 'Paused', actor: 'Coach Duobexiuuu', at: '2026-11-25T02:00:00+00:00', reason: 'Đi công tác' }],
    }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('.meta')?.textContent).toContain('Tạm dừng');
    const row = el.querySelector('.history li')?.textContent ?? '';
    expect(row).toContain('25/11 09:00');
    expect(row).toContain('Đang coaching → Tạm dừng');
    expect(row).toContain('Đi công tác');
    expect(row).toContain('Coach Duobexiuuu');
    expect(el.querySelector('#status-reason')).toBeNull();
    expect(buttons()).toContain('Tiếp tục coaching');
  });

  // @BR-f02-ho-so-hoc-vien-018: lý do rỗng → không gửi
  it('requires a reason before sending', async () => {
    const { fixture, http, el, click } = await setup();

    click('Kết thúc');
    fixture.detectChanges();
    click('Xác nhận');
    fixture.detectChanges();

    http.expectNone('/api/admin/students/s1/coaching-status');
    expect(el.textContent).toContain('Cần nhập lý do đổi trạng thái coaching.');
  });

  // @BR-f02-ho-so-hoc-vien-014: sửa ghi chú tại chỗ, hiện ngày sửa ghi chú
  it('edits the internal note in place', async () => {
    const { fixture, http, el, click } = await setup();
    expect(el.textContent).toContain('Chưa có ghi chú.');

    click('Sửa ghi chú');
    fixture.detectChanges();
    const note = el.querySelector('#internal-note') as HTMLTextAreaElement;
    note.value = 'Tránh lunge sâu vì gối phải';
    note.dispatchEvent(new Event('input'));
    click('Lưu ghi chú');

    const put = http.expectOne('/api/admin/students/s1/internal-note');
    expect(put.request.body).toEqual({ note: 'Tránh lunge sâu vì gối phải', version: 'AAAAAAAAB9E=' });
    put.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải', internalNoteUpdatedAt: '2026-11-23T02:00:00+00:00' }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('.note-text')?.textContent?.trim()).toBe('Tránh lunge sâu vì gối phải');
    expect(el.querySelector('.note-meta')?.textContent?.trim()).toBe('Chỉ coach thấy. Sửa lần cuối 23/11.');
  });

  // @BR-f02-ho-so-hoc-vien-015: hồ sơ đã đổi ở nơi khác → báo và tải lại
  it('asks to reload when the profile was changed elsewhere', async () => {
    const { fixture, http, el, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');
    http.expectOne('/api/admin/students/s1/coaching-status').flush(
      { title: 'Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.', code: 'student.version_conflict' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    click('Tải lại');
    http.expectOne('/api/admin/students/s1').flush(detail({ coachingStatus: 'Ended' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('.meta')?.textContent).toContain('Đã kết thúc');
    expect(el.querySelector('button.reload')).toBeNull();
  });

  it('shows the server message in the status panel for a non-409 error', async () => {
    const { fixture, http, el, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');
    http.expectOne('/api/admin/students/s1/coaching-status').flush(
      { title: 'Không thể tạm dừng học viên này.' }, { status: 400, statusText: 'Bad Request' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('app-coaching-status-panel')?.textContent).toContain('Không thể tạm dừng học viên này.');
    expect(el.querySelector('button.reload')).toBeNull();
  });

  it('clears the note error when the note edit is cancelled', async () => {
    const { fixture, http, el, click } = await setup();

    click('Sửa ghi chú');
    fixture.detectChanges();
    click('Lưu ghi chú');
    http.expectOne('/api/admin/students/s1/internal-note').flush(
      { title: 'Ghi chú quá dài.' }, { status: 400, statusText: 'Bad Request' });
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('.note .msg')?.textContent).toContain('Ghi chú quá dài.');

    click('Huỷ');
    fixture.detectChanges();
    expect(el.querySelector('.note .msg')).toBeNull();
  });

  // @BR-f02-ho-so-hoc-vien-015: 409 không làm mất ghi chú/lý do đã gõ; Tải lại giữ nguyên bản nháp, chỉ làm mới version
  it('keeps the typed note and the editor open on 409, and keeps the draft after reload', async () => {
    const { fixture, http, el, click } = await setup();

    click('Sửa ghi chú');
    fixture.detectChanges();
    const note = el.querySelector('#internal-note') as HTMLTextAreaElement;
    note.value = 'Đau vai trái, tránh overhead';
    note.dispatchEvent(new Event('input'));
    click('Lưu ghi chú');
    http.expectOne('/api/admin/students/s1/internal-note').flush(
      { title: 'Hồ sơ vừa được sửa ở nơi khác.', code: 'student.version_conflict' }, { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('button.reload')).not.toBeNull();
    expect((el.querySelector('#internal-note') as HTMLTextAreaElement).value).toBe('Đau vai trái, tránh overhead');

    click('Tải lại');
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'AAAAAAAB9F=' }));
    await settle();
    fixture.detectChanges();
    expect((el.querySelector('#internal-note') as HTMLTextAreaElement).value).toBe('Đau vai trái, tránh overhead');

    click('Lưu ghi chú');
    const retry = http.expectOne('/api/admin/students/s1/internal-note');
    expect(retry.request.body).toEqual({ note: 'Đau vai trái, tránh overhead', version: 'AAAAAAAB9F=' });
    retry.flush(detail({ internalNote: 'Đau vai trái, tránh overhead' }));
  });

  it('keeps the status panel and typed reason open on 409, and keeps the reason after reload', async () => {
    const { fixture, http, el, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');
    http.expectOne('/api/admin/students/s1/coaching-status').flush(
      { title: 'x', code: 'student.version_conflict' }, { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#status-reason') as HTMLTextAreaElement).value).toBe('Đi công tác');
    click('Tải lại');
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'AAAAAAAB9F=' }));
    await settle();
    fixture.detectChanges();
    expect((el.querySelector('#status-reason') as HTMLTextAreaElement).value).toBe('Đi công tác');
  });
  // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-026: chưa gửi → nút "Gửi thông tin đăng nhập"; gửi xong card cập nhật
  it('sends the login details and shows the new account state and history', async () => {
    const { fixture, http, el, buttons, click } = await setup();
    expect(el.querySelector('app-account-card .state')?.textContent?.trim()).toBe('Chưa gửi thông tin đăng nhập');

    click('Gửi thông tin đăng nhập');
    const post = http.expectOne('/api/admin/students/s1/credentials');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({});
    post.flush(detail({ account: sent, version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('app-account-card .state')?.textContent?.trim())
      .toBe('Đã gửi lúc 25/11 09:00, mật khẩu tạm còn hạn đến 28/11 09:00');
    expect(el.querySelector('app-account-card ol')?.textContent).toContain('Gửi thông tin đăng nhập lần đầu.');
    expect(el.querySelector('p.msg.ok[role="status"]')?.textContent?.trim()).toBe('Đã gửi thông tin đăng nhập.');
    expect(buttons()).toContain('Gửi lại thông tin đăng nhập');
    expect(buttons()).not.toContain('Gửi thông tin đăng nhập');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-006: gửi lỗi → câu báo của server, trạng thái giữ nguyên
  it('shows the server message when the email could not be sent', async () => {
    const { fixture, http, el, click } = await setup(detail({ account: sent }));

    click('Gửi lại thông tin đăng nhập');
    http.expectOne('/api/admin/students/s1/credentials').flush(
      { title: 'Chưa gửi được email. Vui lòng thử lại sau.', code: 'account.email_send_failed' },
      { status: 503, statusText: 'Service Unavailable' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg[role="status"]')?.textContent?.trim()).toBe('Chưa gửi được email. Vui lòng thử lại sau.');
    expect(el.querySelector('p.msg.ok')).toBeNull();
    expect(el.querySelector('app-account-card .state')?.textContent?.trim())
      .toBe('Đã gửi lúc 25/11 09:00, mật khẩu tạm còn hạn đến 28/11 09:00');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-017 @BR-f01-p1-tai-khoan-hoc-vien-026: đã kích hoạt → header "Kích hoạt dd/MM/yyyy", nút gửi link đặt lại
  it('shows the activation date and sends a reset link for an activated account', async () => {
    const { fixture, http, el, buttons, click } = await setup(detail({ accountStatus: 'Activated', account: activated }));

    expect(el.querySelector('.meta .activated')?.textContent?.trim()).toBe('Kích hoạt 13/10/2026');
    expect(el.querySelector('.meta')?.textContent).not.toContain('Chờ kích hoạt');
    expect(buttons()).not.toContain('Gửi lại thông tin đăng nhập');
    expect(el.querySelector('app-account-card ol')?.textContent).toContain('Sửa email từ lan@gmial.com sang lan@gmail.com.');

    click('Gửi link đặt lại mật khẩu');
    const post = http.expectOne('/api/admin/students/s1/password-reset-link');
    expect(post.request.method).toBe('POST');
    post.flush(detail({
      accountStatus: 'Activated',
      account: { ...activated, history: [{ type: 'ResetLinkSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }, ...activated.history] },
    }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg.ok')?.textContent?.trim()).toBe('Đã gửi link đặt lại mật khẩu.');
    expect(el.querySelector('app-account-card ol li')?.textContent).toContain('Gửi link đặt lại mật khẩu.');
  });

  it('shows the notice handed over by the create form', async () => {
    history.replaceState({ notice: 'Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.' }, '');
    try {
      const { el } = await setup();
      expect(el.querySelector('p.msg[role="status"]')?.textContent?.trim())
        .toBe('Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.');
    } finally {
      history.replaceState(null, '');
    }
  });

  it('clears the notice from the history entry after showing it', async () => {
    history.replaceState({ notice: 'Câu báo cũ.' }, '');
    try {
      await setup();
      expect((history.state as { notice?: unknown } | null)?.notice).toBeUndefined();
    } finally {
      history.replaceState(null, '');
    }
  });

  // 409 account.already_activated: trạng thái đổi ở nơi khác → tải lại, nút chuyển sang "Gửi link đặt lại mật khẩu"
  it('reloads the profile on 409 already_activated and switches the button to reset link', async () => {
    const { fixture, http, el, buttons, click } = await setup();

    click('Gửi thông tin đăng nhập');
    http.expectOne('/api/admin/students/s1/credentials').flush(
      { title: 'Tài khoản đã kích hoạt.', code: 'account.already_activated' }, { status: 409, statusText: 'Conflict' });
    await settle();
    http.expectOne('/api/admin/students/s1').flush(detail({ accountStatus: 'Activated', account: activated }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg[role="status"]')?.textContent?.trim()).toBe('Tài khoản đã kích hoạt.');
    expect(buttons()).toContain('Gửi link đặt lại mật khẩu');
    expect(buttons()).not.toContain('Gửi thông tin đăng nhập');
  });
});
