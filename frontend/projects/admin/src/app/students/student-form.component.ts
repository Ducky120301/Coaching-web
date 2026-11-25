import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FORM_SECTIONS, FieldDef, FormPayload, FormValues, toFormValues, toPayload } from './registration-fields';
import { ACCOUNT_STATUS_LABEL } from './student-status';
import { StudentDetail } from './student.models';
import { StudentsApi } from './students.api';

/** @ADR-f02-ho-so-hoc-vien-008 / NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ ở chế độ tạo, chỉ trên trình duyệt này. */
export const DRAFT_KEY = 'dbx.admin.student-draft';

/**
 * @ADR-f02-ho-so-hoc-vien-008: một form cho "Tạo hồ sơ học viên" (hoc-vien/moi) và "Sửa hồ sơ" (hoc-vien/:id/sua).
 * Mục bắt buộc còn thiếu KHÔNG chặn lưu (@BR-f02-ho-so-hoc-vien-004); thanh "x/y mục bắt buộc" chỉ để theo dõi.
 */
@Component({
  selector: 'app-student-form',
  imports: [RouterLink],
  templateUrl: './student-form.component.html',
  styleUrl: './student-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormComponent implements OnInit {
  /** Route data: 'create' | 'edit'. */
  readonly mode = input<'create' | 'edit'>('create');
  /** Route param `:id` (chế độ sửa). */
  readonly id = input<string>();

  private readonly api = inject(StudentsApi);
  private readonly router = inject(Router);

  protected readonly values = signal<FormValues>({});
  protected readonly errors = signal<Record<string, string>>({});
  protected readonly message = signal<string | null>(null);
  protected readonly conflict = signal(false);
  protected readonly saving = signal(false);
  protected readonly loaded = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;

  protected readonly progress = computed(() => {
    const values = this.values();
    const filled = (f: FieldDef) => (values[f.key] ?? '').trim() !== '';
    let requiredAll = 0;
    let requiredDone = 0;
    let all = 0;
    let done = 0;
    const sections = FORM_SECTIONS.map((section) => {
      const required = section.fields.filter((f) => f.required);
      const requiredFilled = required.filter(filled).length;
      const anyFilled = section.fields.filter(filled).length;
      requiredAll += required.length;
      requiredDone += requiredFilled;
      all += section.fields.length;
      done += anyFilled;
      return {
        ...section,
        full: required.length ? requiredFilled === required.length : anyFilled > 0,
        count: required.length ? `${requiredFilled}/${required.length} bắt buộc` : `${anyFilled}/${section.fields.length} mục`,
      };
    });
    return {
      sections,
      totalText: `${requiredDone}/${requiredAll} mục bắt buộc, ${done}/${all} mục tổng`,
      percent: Math.round((100 * requiredDone) / requiredAll),
      complete: requiredDone === requiredAll,
    };
  });

  ngOnInit(): void {
    if (this.isEdit()) void this.load();
    else this.values.set(readDraft());
  }

  protected setValue(key: string, value: string): void {
    const next = { ...this.values(), [key]: value };
    this.values.set(next);
    if (this.errors()[key]) {
      const rest = { ...this.errors() };
      delete rest[key];
      this.errors.set(rest);
    }
    this.message.set(null);
    if (!this.isEdit()) writeDraft(next);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const { payload, errors } = toPayload(this.values());
    this.errors.set(errors);
    if (Object.keys(errors).length > 0) {
      this.message.set('Có ô số liệu chưa đúng. Sửa các ô được đánh dấu đỏ.');
      return;
    }

    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
    try {
      const saved = this.isEdit() ? await this.saveEdit(payload) : await this.saveCreate(payload);
      await this.router.navigateByUrl(`/hoc-vien/${saved.id}`);
    } catch (error) {
      // @BR-f02-ho-so-hoc-vien-015: 409 khi sửa → yêu cầu tải lại; lỗi khác hiện câu báo của server (ProblemDetails.title)
      if (this.isEdit() && error instanceof HttpErrorResponse && error.status === 409) this.conflict.set(true);
      else this.message.set(problemTitle(error));
    } finally {
      this.saving.set(false);
    }
  }

  protected async load(): Promise<void> {
    this.conflict.set(false);
    this.message.set(null);
    try {
      const detail = await firstValueFrom(this.api.get(this.id()!));
      this.loaded.set(detail);
      this.values.set(toFormValues(detail));
      this.errors.set({});
      this.loadFailed.set(false);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected clear(): void {
    clearDraft();
    this.values.set({});
    this.errors.set({});
    this.message.set(null);
  }

  private async saveCreate(payload: FormPayload): Promise<StudentDetail> {
    // Ghi chú nội bộ đi cùng request tạo hồ sơ (một transaction phía server), không gọi thêm endpoint ghi chú
    const created = await firstValueFrom(this.api.create({
      fullName: payload.fullName, email: payload.email, registration: payload.registration,
      ...(payload.internalNote ? { internalNote: payload.internalNote } : {}),
    }));
    clearDraft(); // NFR-f02-ho-so-hoc-vien-004: lưu thành công → xóa bản nháp
    return created;
  }

  private async saveEdit(payload: FormPayload): Promise<StudentDetail> {
    const current = this.loaded()!;
    const saved = await firstValueFrom(this.api.update(current.id, {
      fullName: payload.fullName, registration: payload.registration, version: current.version,
    }));
    if ((payload.internalNote || null) === (current.internalNote ?? null)) return saved;
    return firstValueFrom(this.api.updateNote(saved.id, payload.internalNote, saved.version));
  }
}

function readDraft(): FormValues {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(DRAFT_KEY) ?? '{}');
    return parsed && typeof parsed === 'object' ? (parsed as FormValues) : {};
  } catch {
    return {};
  }
}

function writeDraft(values: FormValues): void {
  try {
    localStorage.setItem(DRAFT_KEY, JSON.stringify(values));
  } catch {
    /* bộ nhớ bị chặn/đầy: bỏ qua, form vẫn dùng được */
  }
}

function clearDraft(): void {
  try {
    localStorage.removeItem(DRAFT_KEY);
  } catch {
    /* bỏ qua */
  }
}

function problemTitle(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.title === 'string') return error.error.title;
  return 'Không lưu được hồ sơ. Vui lòng thử lại.';
}
