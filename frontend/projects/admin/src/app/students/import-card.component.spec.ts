import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { IMPORT_FAILED, IMPORT_HINT, IMPORT_TOO_LARGE, ImportCardComponent } from './import-card.component';
import { ImportResult } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

describe('ImportCardComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(ImportCardComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const http = TestBed.inject(HttpTestingController);
    const importButton = () =>
      [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Import') as HTMLButtonElement;
    const choose = (file: File) => {
      const input = el.querySelector('#import-file') as HTMLInputElement;
      Object.defineProperty(input, 'files', { value: [file], configurable: true });
      input.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    const runImport = async (file: File) => {
      choose(file);
      importButton().click();
      fixture.detectChanges();
      return http.expectOne('/api/admin/students/import');
    };
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    return { el, http, importButton, choose, runImport, render };
  }

  const file = new File(['PK'], 'cau-tra-loi.xlsx', { type: XLSX });

  it('shows the title, the hint and a disabled Import until a file is chosen', () => {
    const { el, importButton, choose } = setup();

    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Import từ Excel');
    expect(el.querySelector('.hint')?.textContent?.trim()).toBe(IMPORT_HINT);
    expect(IMPORT_HINT).toBe('Tải file .xlsx câu trả lời Google Form (tối đa 5 MB, 500 dòng). Hồ sơ được tạo ở Chờ kích hoạt, chưa gửi email.');
    expect(el.querySelector('label[for="import-file"]')?.textContent?.trim()).toBe('Chọn file .xlsx');
    expect((el.querySelector('#import-file') as HTMLInputElement).accept).toContain('.xlsx');
    expect(el.querySelector('.file')?.textContent?.trim()).toBe('Chưa chọn file');
    expect(importButton().disabled).toBe(true);

    choose(file);
    expect(el.querySelector('.file')?.textContent?.trim()).toBe('cau-tra-loi.xlsx');
    expect(importButton().disabled).toBe(false);
  });

  // @BR-f02-import-excel-034: 2 dòng hợp lệ → "Đã tạo 2, bỏ qua 0" (hộp xanh, không bảng)
  it('posts the file as multipart field "file" and shows the summary in a green box when nothing was skipped', async () => {
    const { el, runImport, render, importButton } = setup();
    const request = await runImport(file);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect(((request.request.body as FormData).get('file') as File).name).toBe('cau-tra-loi.xlsx');
    expect(importButton().disabled).toBe(true); // đang gửi
    request.flush({
      created: 2, skipped: 0,
      rows: [
        { row: 2, email: 'lan@gmail.com', status: 'Created', studentId: 's1', errors: [] },
        { row: 3, email: 'quan@gmail.com', status: 'Created', studentId: 's2', errors: [] },
      ],
    } satisfies ImportResult);
    await render();

    const box = el.querySelector('.box')!;
    expect(box.getAttribute('role')).toBe('status');
    expect(box.textContent?.trim()).toBe('Đã tạo 2, bỏ qua 0');
    expect(box.classList.contains('warn')).toBe(false);
    expect(el.querySelector('table')).toBeNull();
    expect(importButton().disabled).toBe(false);
  });

  // @BR-f02-import-excel-034: 3 hợp lệ, 1 trùng email, 1 tuổi sai → "Đã tạo 3, bỏ qua 2" + lý do kèm số dòng Excel
  it('lists every invalid cell of the skipped rows with the Excel row number in a warn box and table', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({
      created: 3, skipped: 2,
      rows: [
        { row: 2, email: 'a@gmail.com', status: 'Created', studentId: 's1', errors: [] },
        { row: 3, email: 'b@gmail.com', status: 'Created', studentId: 's2', errors: [] },
        { row: 4, email: 'quan@gmail.com', status: 'Skipped', studentId: null,
          errors: [{ column: 'Địa chỉ email', reason: 'email đã có hồ sơ' }] },
        { row: 5, email: 'c@gmail.com', status: 'Created', studentId: 's3', errors: [] },
        { row: 6, email: 'an@gmail.com', status: 'Skipped', studentId: null,
          errors: [{ column: 'Tuổi (Age)', reason: 'phải từ 10 đến 100' }, { column: 'Chiều cao (Height)', reason: 'phải từ 100 đến 250' }] },
      ],
    } satisfies ImportResult);
    await render();

    const box = el.querySelector('.box')!;
    expect(box.textContent?.trim()).toBe('Đã tạo 3, bỏ qua 2');
    expect(box.classList.contains('warn')).toBe(true);
    expect([...el.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toEqual(['Dòng', 'Email', 'Cột', 'Lý do']);
    const rows = [...el.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent?.trim()));
    expect(rows).toEqual([
      ['4', 'quan@gmail.com', 'Địa chỉ email', 'email đã có hồ sơ'],
      ['6', 'an@gmail.com', 'Tuổi (Age)', 'phải từ 10 đến 100'],
      ['6', 'an@gmail.com', 'Chiều cao (Height)', 'phải từ 100 đến 250'],
    ]);
  });

  // @BR-f02-import-excel-024: dòng trống email → ô Email hiện "—"
  it('shows a dash when the skipped row has no email', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({
      created: 0, skipped: 1,
      rows: [{ row: 2, email: null, status: 'Skipped', studentId: null, errors: [{ column: 'Địa chỉ email', reason: 'bắt buộc' }] }],
    } satisfies ImportResult);
    await render();

    expect([...el.querySelectorAll('tbody td')].map((td) => td.textContent?.trim())).toEqual(['2', '—', 'Địa chỉ email', 'bắt buộc']);
  });

  // @BR-f02-import-excel-019 (lỗi cả file): hiện title của API, không bảng
  it('shows the API title of a file-level error in a warn box', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({ code: 'import.missing_column', title: 'Thiếu cột "Địa chỉ email".' }, { status: 400, statusText: 'Bad Request' });
    await render();

    const box = el.querySelector('.box')!;
    expect(box.textContent?.trim()).toBe('Thiếu cột "Địa chỉ email".');
    expect(box.classList.contains('warn')).toBe(true);
    expect(el.querySelector('table')).toBeNull();
  });

  // @BR-f02-import-excel-015: 413 của Kestrel/nginx không có ProblemDetails → câu 5 MB; lỗi mạng → câu thử lại
  it('maps a 413 to the 5 MB message, other failures to a retry message, and clears the old result on a new import', async () => {
    const { el, runImport, render } = setup();
    const tooLarge = await runImport(file);
    tooLarge.flush('<html>413</html>', { status: 413, statusText: 'Payload Too Large' });
    await render();
    expect(el.querySelector('.box')?.textContent?.trim()).toBe(IMPORT_TOO_LARGE);
    expect(IMPORT_TOO_LARGE).toBe('File vượt giới hạn 5 MB.');

    const offline = await runImport(file);
    expect(el.querySelector('.box')).toBeNull(); // kết quả cũ bị xoá khi bắt đầu lần import mới
    offline.error(new ProgressEvent('error'));
    await render();
    expect(el.querySelector('.box')?.textContent?.trim()).toBe(IMPORT_FAILED);
    expect(IMPORT_FAILED).toBe('Không import được file. Vui lòng thử lại.');
  });
});
