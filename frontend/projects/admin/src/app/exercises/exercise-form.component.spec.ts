import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it, vi } from 'vitest';
import { ExerciseFormComponent, FIX_FIELDS, VERSION_CONFLICT } from './exercise-form.component';
import { Categories, ExerciseDetail } from './exercise.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const CATS: Categories = {
  muscleGroups: ['Ngực', 'Vai', 'Tay sau', 'Tay trước', 'Lưng', 'Vai sau', 'Đùi trước', 'Đùi sau', 'Mông', 'Bắp chân', 'Bụng']
    .map((name, i) => ({ id: `g${i}`, name })),
  equipment: ['Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']
    .map((name, i) => ({ id: `e${i}`, name })),
};

function detail(overrides: Partial<ExerciseDetail> = {}): ExerciseDetail {
  return {
    id: 'x1', name: 'Lateral Raise', muscleGroupId: 'g1', muscleGroup: 'Vai', equipmentId: 'e1', equipment: 'Cable',
    videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ', hasVideo: true, sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2,
    restSeconds: 90, techniqueNotes: 'Giữ khuỷu hơi cong', commonMistakes: null, isHidden: false, version: 'AAAAAAAAB9E=',
    ...overrides,
  };
}

describe('ExerciseFormComponent', () => {
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
    const fixture = TestBed.createComponent(ExerciseFormComponent);
    fixture.componentRef.setInput('mode', mode);
    if (id) fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const http = TestBed.inject(HttpTestingController);
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    const fill = (selector: string, value: string) => {
      const c = el.querySelector(selector) as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
      c.value = value;
      c.dispatchEvent(new Event(c instanceof HTMLSelectElement ? 'change' : 'input'));
    };
    const value = (selector: string) => (el.querySelector(selector) as HTMLInputElement).value;
    const button = (text: string) => [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement | undefined;
    const save = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      fixture.detectChanges();
    };
    const fillValid = () => {
      fill('#ex-name', 'Lateral Raise');
      fill('#ex-muscle', 'g1');
      fill('#ex-equipment', 'e2');
    };
    return { fixture, el, http, render, fill, value, button, save, fillValid, navigate };
  }

  async function created() {
    const s = setup('create');
    s.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    await s.render();
    return s;
  }

  async function edited() {
    const s = setup('edit', 'x1');
    s.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    s.http.expectOne('/api/admin/exercises/x1').flush(detail());
    await s.render();
    return s;
  }

  // @ADR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-001
  it('creates an exercise from the typed dose text and opens it in the library', async () => {
    const { el, http, render, fill, save, navigate } = await created();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Thêm bài mới');
    expect(el.querySelector('.crumbs a')?.getAttribute('href')).toBe('/thu-vien/bai-tap');
    expect([...el.querySelectorAll('#ex-muscle option')].map((o) => o.textContent?.trim()))
      .toEqual(['Chọn…', ...CATS.muscleGroups.map((g) => g.name)]);
    expect([...el.querySelectorAll('#ex-equipment option')].map((o) => o.textContent?.trim()))
      .toEqual(['Chọn…', ...CATS.equipment.map((e) => e.name)]);
    expect([...el.querySelectorAll('label')].map((l) => l.textContent?.replace(' *', '').trim())).toEqual([
      'Tên bài', 'Nhóm cơ', 'Dụng cụ', 'Link video YouTube', 'Sets', 'Reps', 'RIR', 'Nghỉ', 'Hướng dẫn kỹ thuật', 'Lỗi hay gặp',
    ]);

    fill('#ex-name', '  Lateral Raise ');
    fill('#ex-muscle', 'g1');
    fill('#ex-equipment', 'e2');
    fill('#ex-video', 'https://youtube.com/shorts/lMJUXEvcMkQ');
    fill('#ex-sets', '3');
    fill('#ex-reps', '12-15');
    fill('#ex-rir', '0–1');
    fill('#ex-rest', '60');
    fill('#ex-technique', 'Giữ khuỷu hơi cong, kéo bằng vai');
    save();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercises' });
    expect(req.request.body).toEqual({
      name: 'Lateral Raise', muscleGroupId: 'g1', equipmentId: 'e2', videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ',
      sets: 3, repsMin: 12, repsMax: 15, rirMin: 0, rirMax: 1, restSeconds: 60,
      techniqueNotes: 'Giữ khuỷu hơi cong, kéo bằng vai', commonMistakes: null,
    });
    req.flush(detail({ id: 'new1' }), { status: 201, statusText: 'Created' });
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=new1');
  });

  // @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
  it('marks missing name, muscle group, equipment and an unreadable dose without calling the API', async () => {
    const { fixture, el, http, fill, save } = await created();

    fill('#ex-name', '   ');
    fill('#ex-reps', 'abc');
    fill('#ex-rest', '90 giây');
    save();

    http.expectNone({ method: 'POST', url: '/api/admin/exercises' });
    expect([...el.querySelectorAll('.field-err')].map((e) => e.textContent?.trim())).toEqual([
      'Nhập tên bài.', 'Chọn nhóm cơ.', 'Chọn dụng cụ.', 'Nhập một số hoặc khoảng, VD: 10–12.', 'Nhập số giây, VD: 90.',
    ]);
    expect(el.querySelector('#ex-name')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('#ex-name')?.classList.contains('err')).toBe(true);
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe(FIX_FIELDS);

    fill('#ex-name', 'Lateral Raise');
    fixture.detectChanges();
    expect(el.querySelector('#ex-name')?.classList.contains('err')).toBe(false);
  });

  // @BR-f03-thu-vien-bai-tap-010: khoảng do API kiểm, lỗi gắn vào đúng ô
  it('puts the server message under the field named by the error code', async () => {
    const { el, http, render, fill, save, fillValid } = await created();
    fillValid();
    fill('#ex-reps', '12–10');
    save();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercises' });
    expect(req.request.body.repsMin).toBe(12);
    expect(req.request.body.repsMax).toBe(10);
    req.flush({ code: 'exercise.reps_invalid', title: 'Reps là một số hoặc khoảng thấp–cao trong 1–100.' },
      { status: 400, statusText: 'Bad Request' });
    await render();

    const reps = el.querySelector('#ex-reps')!.closest('.field')!;
    expect(reps.querySelector('.field-err')?.textContent?.trim()).toBe('Reps là một số hoặc khoảng thấp–cao trong 1–100.');
    expect(el.querySelector('#ex-reps')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe(FIX_FIELDS);
  });

  // @BR-f03-thu-vien-bai-tap-001: trùng bài đang ẩn → gợi ý bỏ ẩn bài đó
  it('offers to unhide the hidden duplicate and opens it after unhiding', async () => {
    const { el, http, render, save, fillValid, button, navigate } = await created();
    fillValid();
    save();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises' }).flush(
      { code: 'exercise.duplicate', title: 'Đã có bài cùng tên và dụng cụ.', hiddenExerciseId: 'h1' },
      { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent).toContain('Đã có bài cùng tên và dụng cụ. Bài đó đang ẩn.');
    button('Bỏ ẩn bài đó')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/h1/unhide' }).flush(detail({ id: 'h1' }));
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=h1');
  });

  // @BR-f03-thu-vien-bai-tap-001
  it('shows the duplicate message without an unhide action when the duplicate is visible', async () => {
    const { el, http, render, save, fillValid, button } = await created();
    fillValid();
    save();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises' }).flush(
      { code: 'exercise.duplicate', title: 'Đã có bài cùng tên và dụng cụ.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã có bài cùng tên và dụng cụ.');
    expect(button('Bỏ ẩn bài đó')).toBeUndefined();
  });

  // @BR-f03-thu-vien-bai-tap-020
  it('loads the exercise into the form and saves it with its version', async () => {
    const { el, http, render, fill, value, save, navigate } = await edited();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa bài');
    expect(value('#ex-name')).toBe('Lateral Raise');
    expect(value('#ex-muscle')).toBe('g1');
    expect(value('#ex-equipment')).toBe('e1');
    expect(value('#ex-video')).toBe('https://youtube.com/shorts/lMJUXEvcMkQ');
    expect(value('#ex-sets')).toBe('3');
    expect(value('#ex-reps')).toBe('10–12');
    expect(value('#ex-rir')).toBe('1–2');
    expect(value('#ex-rest')).toBe('90');
    expect(value('#ex-technique')).toBe('Giữ khuỷu hơi cong');
    expect(value('#ex-mistakes')).toBe('');
    expect(el.querySelector('a.cancel')?.getAttribute('href')).toBe('/thu-vien/bai-tap?bai=x1');

    fill('#ex-mistakes', 'Nhún vai khi kéo');
    save();
    const req = http.expectOne({ method: 'PUT', url: '/api/admin/exercises/x1' });
    expect(req.request.body).toEqual({
      name: 'Lateral Raise', muscleGroupId: 'g1', equipmentId: 'e1', videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ',
      sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2, restSeconds: 90,
      techniqueNotes: 'Giữ khuỷu hơi cong', commonMistakes: 'Nhún vai khi kéo', version: 'AAAAAAAAB9E=',
    });
    req.flush(detail({ commonMistakes: 'Nhún vai khi kéo', version: 'AAAAAAAAB9I=' }));
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=x1');
  });

  // @BR-f03-thu-vien-bai-tap-020
  it('reports a version conflict and reloads the latest exercise on "Tải lại"', async () => {
    const { el, http, render, fill, value, save, button } = await edited();
    fill('#ex-name', 'Lateral Raise (bản của tôi)');
    save();
    http.expectOne({ method: 'PUT', url: '/api/admin/exercises/x1' }).flush(
      { code: 'exercise.version_conflict', title: VERSION_CONFLICT }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent).toContain('Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    button('Tải lại')!.click();
    http.expectOne('/api/admin/exercises/x1').flush(detail({ name: 'Cable Lateral Raise', version: 'AAAAAAAAB9I=' }));
    await render();

    expect(value('#ex-name')).toBe('Cable Lateral Raise');
    expect(el.querySelector('.msg')).toBeNull();
  });

  // @BR-f03-thu-vien-bai-tap-006
  it('adds a muscle group inline and selects it', async () => {
    const { el, http, render, fill, button } = await created();

    button('+ Thêm nhóm cơ')!.click();
    await render();
    expect(el.querySelector('label[for="new-MuscleGroup"]')?.textContent?.trim()).toBe('Tên nhóm cơ mới');
    fill('#new-MuscleGroup', 'Cẳng tay');
    button('Thêm')!.click();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' });
    expect(req.request.body).toEqual({ kind: 'MuscleGroup', name: 'Cẳng tay' });
    req.flush({ id: 'g11', kind: 'MuscleGroup', name: 'Cẳng tay' }, { status: 201, statusText: 'Created' });
    await render();

    expect(el.querySelector('#new-MuscleGroup')).toBeNull();
    const select = el.querySelector('#ex-muscle') as HTMLSelectElement;
    expect(select.value).toBe('g11');
    expect(select.selectedOptions[0].textContent?.trim()).toBe('Cẳng tay');
    expect(button('+ Thêm nhóm cơ')).toBeDefined();
  });

  // @BR-f03-thu-vien-bai-tap-006
  it('keeps the inline box open with the server message when the category already exists', async () => {
    const { el, http, render, fill, button } = await created();

    button('+ Thêm dụng cụ')!.click();
    await render();
    button('Thêm')!.click();
    await render();
    expect(el.querySelector('#new-Equipment')!.closest('.field')!.textContent).toContain('Nhập tên mục.');
    http.expectNone({ method: 'POST', url: '/api/admin/exercise-categories' });

    fill('#new-Equipment', 'cable');
    button('Thêm')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' }).flush(
      { code: 'category.duplicate', title: 'Đã có mục cùng tên.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('#new-Equipment')).not.toBeNull();
    expect(el.querySelector('#new-Equipment')!.closest('.field')!.textContent).toContain('Đã có mục cùng tên.');
  });

  it('shows a load error instead of the form when the exercise cannot be loaded', async () => {
    const { el, http, render } = setup('edit', 'zz');
    http.expectOne('/api/admin/exercise-categories').flush(CATS);
    http.expectOne('/api/admin/exercises/zz').flush({ code: 'exercise.not_found', title: 'Không tìm thấy bài.' }, { status: 404, statusText: 'Not Found' });
    await render();

    expect(el.querySelector('form')).toBeNull();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Không tải được bài tập. Vui lòng tải lại trang.');
  });
});
