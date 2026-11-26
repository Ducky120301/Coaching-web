import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { formatVnTime } from './format';
import { FORM_SECTIONS, FieldDef, FormPayload, FormValues, toFormValues, toPayload } from './registration-fields';
import { ACCOUNT_STATUS_LABEL } from './student-status';
import { StudentDetail } from './student.models';
import { StudentsApi } from './students.api';
import { ImportCardComponent } from './import-card.component';
import { ReceiveCardComponent } from './receive-card.component';

/** @ADR-f02-ho-so-hoc-vien-008 / NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ ở chế độ tạo, chỉ trên trình duyệt này. */
export const DRAFT_KEY = 'dbx.admin.student-draft';
/** Câu báo nguyên văn prototype AdTaoHoSo / tech-design F01-P1 "Thiết kế giao diện (UI)". */
export const SENT_MESSAGE = 'Đã lưu hồ sơ và gửi thông tin đăng nhập.';
export const SAVED_NOT_SENT_MESSAGE = 'Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.';
export const EMAIL_CHANGE_NOTE = 'Đổi email sẽ huỷ mật khẩu tạm cũ và gửi thông tin đăng nhập mới tới email mới.';

type Tone = 'ok' | 'warn';

/**
 * @ADR-f02-ho-so-hoc-vien-008: một form cho "Tạo hồ sơ học viên" (hoc-vien/moi) và "Sửa hồ sơ" (hoc-vien/:id/sua).
 * "Chỉ lưu hồ sơ": mục bắt buộc còn thiếu KHÔNG chặn lưu (@BR-f02-ho-so-hoc-vien-004).
 * "Lưu và gửi thông tin đăng nhập": cần đủ mục bắt buộc (@BR-f01-p1-tai-khoan-hoc-vien-002), tạo rồi gửi.
 */
@Component({
  selector: 'app-student-form',
  imports: [RouterLink, ReceiveCardComponent, ImportCardComponent],
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
  protected readonly message = signal<{ tone: Tone; text: string } | null>(null);
  protected readonly conflict = signal(false);
  protected readonly saving = signal(false);
  protected readonly tried = signal(false);
  protected readonly loaded = signal<StudentDetail | null>(null);
  protected readonly created = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly emailChangeNote = EMAIL_CHANGE_NOTE;

  /** Sửa hồ sơ: email sửa được khi tài khoản còn Chờ kích hoạt (@BR-f01-p1-tai-khoan-hoc-vien-007). */
  protected readonly emailEditable = computed(() => !this.isEdit() || this.loaded()?.accountStatus === 'PendingActivation');
  /** Ghi chú dưới ô email khi hồ sơ đã từng được gửi thông tin (@BR-f01-p1-tai-khoan-hoc-vien-009). */
  protected readonly showEmailChangeNote = computed(
    () => this.isEdit() && this.emailEditable() && this.loaded()?.account.credentialsSentAt != null);
  /** Aside "Trạng thái tài khoản" sau khi Lưu và gửi (stText/stNote của prototype). */
  protected readonly sentState = computed(() => {
    const c = this.created();
    return c?.account.credentialsSentAt ? { email: c.email, time: formatVnTime(c.account.credentialsSentAt) } : null;
  });

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
      missing: requiredAll - requiredDone,
    };
  });

  ngOnInit(): void {
    if (this.isEdit()) void this.load();
    else this.values.set(readDraft());
  }

  protected isMissing(f: FieldDef): boolean {
    return this.tried() && f.required && (this.values()[f.key] ?? '').trim() === '';
  }

  protected setValue(key: string, value: string): void {
    const next = { ...this.values(), [key]: value };
    this.values.set(next);
    if (this.errors()[key]) {
      const rest = { ...this.errors() };
      delete rest[key];
      this.errors.set(rest);
    }
    // Đã tạo + gửi (created): giữ câu báo xanh, không ghi lại bản nháp cho học viên đã có hồ sơ
    if (this.created()) return;
    this.message.set(null);
    if (!this.isEdit()) writeDraft(next);
  }

  /** Nút chính: tạo → "Lưu và gửi thông tin đăng nhập"; sửa → "Lưu thay đổi". */
  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.isEdit()) await this.save();
    else await this.saveAndSend();
  }

  /** "Chỉ lưu hồ sơ" (F02): lưu rồi mở hồ sơ. */
  protected async saveOnly(): Promise<void> {
    await this.save();
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
    this.tried.set(false);
    this.created.set(null);
  }

  private async save(): Promise<void> {
    if (this.isEdit() && !this.loaded()) return; // chưa tải xong hồ sơ: chưa có version để gửi
    const payload = this.validPayload();
    if (!payload) return;
    this.begin();
    try {
      const saved = this.isEdit() ? await this.saveEdit(payload) : await this.saveCreate(payload);
      await this.router.navigateByUrl(`/hoc-vien/${saved.id}`);
    } catch (error) {
      // @BR-f02-ho-so-hoc-vien-015: chỉ xung đột phiên bản mới hiện "Tải lại"; 409 khác (email đã dùng/đã khoá) hiện câu của server
      if (this.isEdit() && isVersionConflict(error)) this.conflict.set(true);
      else this.warn(problemTitle(error));
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc → hồ sơ VẪN được lưu (spec), chưa gửi;
  // sang trang hồ sơ kèm câu báo số mục thiếu (trang hồ sơ có khối "Mục bắt buộc còn thiếu" của F02)
  private async saveAndSend(): Promise<void> {
    const missing = this.progress().missing;
    if (missing > 0) {
      this.tried.set(true);
      const draft = this.validPayload();
      if (!draft) return;
      this.begin();
      try {
        const saved = await this.saveCreate(draft);
        await this.router.navigateByUrl(`/hoc-vien/${saved.id}`, {
          state: { notice: `Đã lưu hồ sơ. Còn ${missing} mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.` },
        });
      } catch (error) {
        this.warn(problemTitle(error));
      } finally {
        this.saving.set(false);
      }
      return;
    }
    const payload = this.validPayload();
    if (!payload) return;
    this.begin();
    let created: StudentDetail;
    try {
      created = await this.saveCreate(payload);
    } catch (error) {
      this.warn(problemTitle(error));
      this.saving.set(false);
      return;
    }
    try {
      this.created.set(await firstValueFrom(this.api.sendCredentials(created.id)));
      this.message.set({ tone: 'ok', text: SENT_MESSAGE });
    } catch {
      // @BR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ đã lưu, thông tin chưa gửi → sang trang hồ sơ để coach gửi lại
      await this.router.navigateByUrl(`/hoc-vien/${created.id}`, { state: { notice: SAVED_NOT_SENT_MESSAGE } });
    } finally {
      this.saving.set(false);
    }
  }

  private validPayload(): FormPayload | null {
    const { payload, errors } = toPayload(this.values());
    this.errors.set(errors);
    if (Object.keys(errors).length > 0) {
      this.warn('Có ô số liệu chưa đúng. Sửa các ô được đánh dấu đỏ.');
      return null;
    }
    return payload;
  }

  private begin(): void {
    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
  }

  private warn(text: string): void {
    this.message.set({ tone: 'warn', text });
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
    let current = this.loaded()!;
    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009: đổi email đi endpoint riêng trước
    // (đã gửi thông tin → server gửi thông tin mới tới email mới), rồi PUT hồ sơ với version mới
    if (this.emailEditable() && payload.email.toLowerCase() !== current.email.toLowerCase()) {
      current = await firstValueFrom(this.api.changeEmail(current.id, payload.email, current.version));
      this.loaded.set(current);
    }
    const saved = await firstValueFrom(this.api.update(current.id, {
      fullName: payload.fullName, registration: payload.registration, version: current.version,
    }));
    this.loaded.set(saved); // PUT hồ sơ thành công: giữ version mới để thử lại ghi chú không bị 409 giả
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

function isVersionConflict(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 409 && error.error?.code === 'student.version_conflict';
}

function problemTitle(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.title === 'string') return error.error.title;
  return 'Không lưu được hồ sơ. Vui lòng thử lại.';
}
