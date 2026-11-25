import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { StudentProfileComponent } from './student-profile.component';
import { StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-20T02:00:00+00:00',
    registration: { trainingGoal: 'Giảm mỡ, giữ cơ', availability: 'T3–T7 buổi tối', equipment: null, weightKg: 72.5 },
    missingRequired: ['pack', 'photoBackUrl'], updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=',
    internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
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
});
