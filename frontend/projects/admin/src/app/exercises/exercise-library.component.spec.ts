import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ExerciseLibraryComponent, NO_NOTES, NO_VIDEO } from './exercise-library.component';
import { Categories, ExerciseDetail, ExerciseListItem } from './exercise.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const CATS: Categories = {
  muscleGroups: ['Ngực', 'Vai', 'Tay sau', 'Tay trước', 'Lưng', 'Vai sau', 'Đùi trước', 'Đùi sau', 'Mông', 'Bắp chân', 'Bụng']
    .map((name, i) => ({ id: `g${i}`, name })),
  equipment: ['Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']
    .map((name, i) => ({ id: `e${i}`, name })),
};

const ROWS: ExerciseListItem[] = [
  { id: 'x1', name: '45° Incline Chest Fly', muscleGroup: 'Ngực', equipment: 'Cable', hasVideo: true, isHidden: false },
  { id: 'x2', name: 'Barbell Bench Press', muscleGroup: 'Ngực', equipment: 'Barbell', hasVideo: true, isHidden: false },
  { id: 'x3', name: 'Lateral Raise', muscleGroup: 'Vai', equipment: 'Cable', hasVideo: false, isHidden: false },
];

function detail(overrides: Partial<ExerciseDetail> = {}): ExerciseDetail {
  return {
    id: 'x1', name: '45° Incline Chest Fly', muscleGroupId: 'g0', muscleGroup: 'Ngực', equipmentId: 'e1', equipment: 'Cable',
    videoUrl: 'https://www.youtube.com/shorts/PfmfAeMpym4', hasVideo: true, sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2,
    restSeconds: 90, techniqueNotes: null, commonMistakes: null, isHidden: false, version: 'AAAAAAAAB9E=',
    ...overrides,
  };
}

describe('ExerciseLibraryComponent', () => {
  afterEach(() => vi.restoreAllMocks());

  function mount(bai?: string) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(ExerciseLibraryComponent);
    if (bai) fixture.componentRef.setInput('bai', bai);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    const chip = (label: string) =>
      [...el.querySelectorAll('button.chip')].find((b) => b.textContent?.trim() === label) as HTMLButtonElement;
    const button = (label: string) =>
      [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === label) as HTMLButtonElement | undefined;
    return { fixture, el, http: TestBed.inject(HttpTestingController), render, chip, button };
  }

  /** Danh mục → danh sách mặc định (không lọc); chưa trả chi tiết bài đầu. */
  async function setup(bai?: string) {
    const m = mount(bai);
    m.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    await settle();
    m.http.expectOne('/api/admin/exercises').flush(ROWS);
    await settle();
    return m;
  }

  it('shows the title, one chip per muscle group with its count, the hidden chip, equipment chips and the table', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Thư viện bài tập');
    expect(el.querySelector('.summary')?.textContent?.trim()).toBe('3 bài, 1 bài thiếu video mẫu.');
    const groups = [...el.querySelectorAll('[role=tablist][aria-label="Nhóm cơ"] button')].map((b) => b.textContent?.trim());
    expect(groups).toEqual([
      'Tất cả (3)', 'Ngực (2)', 'Vai (1)', 'Tay sau (0)', 'Tay trước (0)', 'Lưng (0)', 'Vai sau (0)',
      'Đùi trước (0)', 'Đùi sau (0)', 'Mông (0)', 'Bắp chân (0)', 'Bụng (0)', 'Đã ẩn',
    ]);
    expect(el.querySelector('[role=tab][aria-selected=true]')?.textContent?.trim()).toBe('Tất cả (3)');
    const equips = [...el.querySelectorAll('[role=group][aria-label="Dụng cụ"] button')].map((b) => b.textContent?.trim());
    expect(equips).toEqual(['Tất cả', 'Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']);
    expect(el.querySelector('label[for=q]')?.textContent?.trim()).toBe('Tìm bài tập');
    expect((el.querySelector('#q') as HTMLInputElement).placeholder).toBe('Tìm theo tên bài, gõ xong bấm Enter');

    expect([...el.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toEqual(['Bài tập', 'Nhóm cơ', 'Dụng cụ', 'Video mẫu']);
    const rows = [...el.querySelectorAll('tbody tr')];
    expect(rows).toHaveLength(3);
    expect(rows[0].querySelector('th[scope=row]')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(rows[0].querySelector('.video')?.textContent?.trim()).toBe('Xem video');
    expect(rows[2].querySelector('.video')?.textContent?.trim()).toBe('Thiếu video');
    expect(rows[2].querySelector('.video')?.classList.contains('missing')).toBe(true);
    expect(el.querySelector('a.create')?.getAttribute('href')).toBe('/thu-vien/bai-tap/moi');
    expect(el.textContent).not.toContain('Đang dùng trong');
    expect(el.textContent).not.toContain('Thêm vào giáo án');
  });

  // @ADR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-006
  it('shows the first exercise in the aside with a nocookie 9:16 embed for shorts and the formatted dose', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelector('tbody tr.on button.pick')?.getAttribute('aria-pressed')).toBe('true');
    const aside = el.querySelector('aside')!;
    expect(aside.querySelector('.meta')?.textContent?.trim()).toBe('Ngực, Cable');
    expect(aside.querySelector('h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    const iframe = aside.querySelector('iframe')!;
    expect(iframe.getAttribute('src')).toBe('https://www.youtube-nocookie.com/embed/PfmfAeMpym4?rel=0&modestbranding=1&playsinline=1');
    expect(iframe.getAttribute('title')).toBe('Video mẫu 45° Incline Chest Fly');
    expect(aside.querySelector('.frame')?.classList.contains('short')).toBe(true);
    expect(aside.querySelector('a.yt')?.getAttribute('href')).toBe('https://www.youtube.com/shorts/PfmfAeMpym4');
    expect(aside.querySelector('a.yt')?.textContent?.trim()).toBe('Mở trên YouTube');
    const dose = [...aside.querySelectorAll('dl > div')].map((d) => [d.querySelector('dt')?.textContent?.trim(), d.querySelector('dd')?.textContent?.trim()]);
    expect(dose).toEqual([['Sets', '3'], ['Reps', '10–12'], ['RIR', '1–2'], ['Nghỉ', '90s']]);
    expect(aside.querySelector('.box')?.textContent?.trim()).toBe(NO_NOTES);
    expect(aside.querySelector('a.edit')?.getAttribute('href')).toBe('/thu-vien/bai-tap/x1/sua');
    expect([...aside.querySelectorAll('.actions button')].map((b) => b.textContent?.trim())).toEqual(['Ẩn bài', 'Xoá bài']);
  });

  it('shows a watch video in a 16:9 frame and the technique notes and common mistakes', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    (el.querySelectorAll('tbody button.pick')[1] as HTMLButtonElement).click();
    http.expectOne('/api/admin/exercises/x2').flush(detail({
      id: 'x2', name: 'Barbell Bench Press', equipmentId: 'e3', equipment: 'Barbell', videoUrl: 'https://www.youtube.com/watch?v=vcBig73ojpE',
      techniqueNotes: 'Siết bả vai, hạ thanh chạm ngực dưới', commonMistakes: 'Nảy thanh trên ngực',
    }));
    await render();

    const aside = el.querySelector('aside')!;
    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('Barbell Bench Press');
    expect(aside.querySelector('iframe')?.getAttribute('src')).toBe('https://www.youtube-nocookie.com/embed/vcBig73ojpE?rel=0&modestbranding=1&playsinline=1');
    expect(aside.querySelector('.frame')?.classList.contains('short')).toBe(false);
    const notes = [...aside.querySelectorAll('section.notes')].map((s) => [s.querySelector('h3')?.textContent?.trim(), s.querySelector('p')?.textContent?.trim()]);
    expect(notes).toEqual([['Hướng dẫn kỹ thuật', 'Siết bả vai, hạ thanh chạm ngực dưới'], ['Lỗi hay gặp', 'Nảy thanh trên ngực']]);
    expect(aside.querySelector('.box')).toBeNull();
  });

  // @BR-f03-thu-vien-bai-tap-008
  it('says the video is missing and shows — for an empty dose', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    (el.querySelectorAll('tbody button.pick')[2] as HTMLButtonElement).click();
    http.expectOne('/api/admin/exercises/x3').flush(detail({
      id: 'x3', name: 'Lateral Raise', muscleGroupId: 'g1', muscleGroup: 'Vai', videoUrl: null, hasVideo: false,
      sets: null, repsMin: null, repsMax: null, rirMin: null, rirMax: null, restSeconds: null,
    }));
    await render();

    const aside = el.querySelector('aside')!;
    expect(aside.querySelector('iframe')).toBeNull();
    expect(aside.querySelector('a.yt')).toBeNull();
    expect(aside.textContent).toContain(NO_VIDEO);
    expect([...aside.querySelectorAll('dd')].map((d) => d.textContent?.trim())).toEqual(['—', '—', '—', '—']);
    expect(aside.querySelector('.rx')?.textContent?.trim()).toBe('Chưa có liều mặc định.');
  });

  // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
  it('combines muscle group, equipment and keyword, and lists hidden exercises under "Đã ẩn"', async () => {
    const { el, http, render, chip } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    chip('Ngực (2)').click();
    http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.get('muscleGroupId') === 'g0' && !r.params.has('hidden'))
      .flush([ROWS[0], ROWS[1]]);
    await render();
    expect(chip('Ngực (2)').getAttribute('aria-selected')).toBe('true');
    expect(chip('Ngực (2)').classList.contains('on')).toBe(true);

    chip('Cable').click();
    http.expectOne((r) => r.params.get('muscleGroupId') === 'g0' && r.params.get('equipmentId') === 'e1').flush([ROWS[0]]);
    await render();
    expect(chip('Cable').getAttribute('aria-pressed')).toBe('true');

    const q = el.querySelector('#q') as HTMLInputElement;
    q.value = 'fly';
    q.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    http.expectOne((r) => r.params.get('q') === 'fly' && r.params.get('muscleGroupId') === 'g0' && r.params.get('equipmentId') === 'e1')
      .flush([ROWS[0]]);
    await render();
    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(el.querySelector('.summary')?.textContent?.trim()).toBe('3 bài, 1 bài thiếu video mẫu. Đang hiện 1 bài.');

    chip('Đã ẩn').click();
    http.expectOne((r) => r.params.get('hidden') === 'true' && !r.params.has('muscleGroupId')
      && r.params.get('equipmentId') === 'e1' && r.params.get('q') === 'fly').flush([]);
    await render();
    expect(el.querySelector('table')).toBeNull();
    expect(el.querySelector('aside')).toBeNull();
    expect(el.querySelector('.empty')?.textContent?.trim()).toBe('Không tìm thấy bài phù hợp.');
  });

  // @BR-f03-thu-vien-bai-tap-015
  it('hides the selected exercise, keeps it in the aside with "Bỏ ẩn", and unhides it again', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    button('Ẩn bài')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/hide' }).flush(detail({ isHidden: true }));
    await settle();
    http.expectOne('/api/admin/exercises').flush([ROWS[1], ROWS[2]]);
    await render();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelector('aside dbx-status-badge')?.textContent?.trim()).toBe('Đã ẩn');
    expect(button('Bỏ ẩn')).toBeDefined();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã ẩn bài "45° Incline Chest Fly". Xem lại ở mục Đã ẩn.');
    expect(el.querySelector('[role=tab][aria-selected=true]')?.textContent?.trim()).toBe('Tất cả (2)');

    button('Bỏ ẩn')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/unhide' }).flush(detail());
    await settle();
    http.expectOne('/api/admin/exercises').flush(ROWS);
    await render();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(3);
    expect(button('Ẩn bài')).toBeDefined();
    expect(el.querySelector('aside dbx-status-badge')).toBeNull();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã bỏ ẩn bài "45° Incline Chest Fly".');
  });

  // @BR-f03-thu-vien-bai-tap-017
  it('deletes after confirmation and selects the first remaining exercise', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);

    button('Xoá bài')!.click();
    expect(confirm).toHaveBeenCalledWith('Xoá bài "45° Incline Chest Fly" (Cable) khỏi thư viện? Không hoàn tác được.');
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' }).flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    http.expectOne('/api/admin/exercises').flush([ROWS[1], ROWS[2]]);
    await settle();
    http.expectOne('/api/admin/exercises/x2').flush(detail({ id: 'x2', name: 'Barbell Bench Press', equipment: 'Barbell' }));
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã xoá bài "45° Incline Chest Fly".');
    expect(el.querySelector('.msg')?.classList.contains('ok')).toBe(true);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('Barbell Bench Press');
  });

  // @BR-f03-thu-vien-bai-tap-017
  it('keeps the exercise when deletion is cancelled or refused because it is in use', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);

    button('Xoá bài')!.click();
    http.expectNone({ method: 'DELETE', url: '/api/admin/exercises/x1' });

    confirm.mockReturnValue(true);
    button('Xoá bài')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' })
      .flush({ code: 'exercise.in_use', title: 'Bài đang có trong giáo án, chỉ ẩn được.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Bài đang có trong giáo án, chỉ ẩn được.');
    expect(el.querySelector('.msg')?.classList.contains('ok')).toBe(false);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelectorAll('tbody tr')).toHaveLength(3);
  });

  it('preselects the exercise given by ?bai=', async () => {
    const { el, http, render } = await setup('x3');
    http.expectNone('/api/admin/exercises/x1');
    http.expectOne('/api/admin/exercises/x3').flush(detail({ id: 'x3', name: 'Lateral Raise' }));
    await render();

    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('Lateral Raise');
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('Lateral Raise');
  });

  it('shows an error line when the library cannot be loaded', async () => {
    const { el, http, render } = mount();
    http.expectOne('/api/admin/exercise-categories').flush('boom', { status: 500, statusText: 'Server Error' });
    await render();

    expect(el.querySelector('[role=alert]')?.textContent?.trim()).toBe('Không tải được thư viện bài tập. Vui lòng tải lại trang.');
  });
});
