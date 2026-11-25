import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatusBadgeComponent, initialsOf } from '@coaching/shared';
import { Observable, firstValueFrom } from 'rxjs';
import { AccountCardComponent } from './account-card.component';
import { CoachingStatusPanelComponent } from './coaching-status-panel.component';
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime, formatVnFullDate } from './format';
import { FIELD_LABEL, FORM_SECTIONS, FieldDef } from './registration-fields';
import { ACCOUNT_STATUS_LABEL, ACCOUNT_STATUS_TONE, COACHING_STATUS_LABEL, COACHING_STATUS_TONE } from './student-status';
import { AccountState, CoachingStatus, StudentDetail } from './student.models';
import { StudentsApi } from './students.api';

interface StatusAction {
  label: string;
  to: CoachingStatus;
}

/** Nút theo trạng thái hiện tại — đổi được theo mọi chiều (@BR-f02-ho-so-hoc-vien-019), không đổi sang chính nó (@BR-f02-ho-so-hoc-vien-017). */
const ACTIONS: Record<CoachingStatus, StatusAction[]> = {
  Active: [{ label: 'Tạm dừng', to: 'Paused' }, { label: 'Kết thúc', to: 'Ended' }],
  Paused: [{ label: 'Tiếp tục coaching', to: 'Active' }, { label: 'Kết thúc', to: 'Ended' }],
  Ended: [{ label: 'Tiếp tục coaching', to: 'Active' }],
};

interface AccountAction {
  label: string;
  kind: 'credentials' | 'reset';
  done: string;
}

/** Nút phụ theo trạng thái tài khoản (tech-design F01-P1 UI): chưa gửi / đã gửi, hết hạn / đã kích hoạt. */
const ACCOUNT_ACTIONS: Record<AccountState, AccountAction> = {
  NotSent: { label: 'Gửi thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi thông tin đăng nhập.' },
  Sent: { label: 'Gửi lại thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi lại thông tin đăng nhập.' },
  TempPasswordExpired: { label: 'Gửi lại thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi lại thông tin đăng nhập.' },
  Activated: { label: 'Gửi link đặt lại mật khẩu', kind: 'reset', done: 'Đã gửi link đặt lại mật khẩu.' },
};

/** Mục form hiển thị ở "Xem đủ form đăng ký" (bỏ họ tên, email — đã ở header — và nhóm ghi chú). */
const REGISTRATION_SECTIONS = FORM_SECTIONS.filter((s) => s.id !== 'nb').map((s) => ({
  ...s,
  fields: s.fields.filter((f) => f.key !== 'fullName' && f.key !== 'email'),
}));

@Component({
  selector: 'app-student-profile',
  imports: [RouterLink, StatusBadgeComponent, CoachingStatusPanelComponent, AccountCardComponent],
  templateUrl: './student-profile.component.html',
  styleUrl: './student-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentProfileComponent implements OnInit {
  /** Route param `:id`. */
  readonly id = input.required<string>();

  private readonly api = inject(StudentsApi);

  protected readonly detail = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly conflict = signal(false);
  protected readonly busy = signal(false);
  protected readonly pending = signal<StatusAction | null>(null);
  protected readonly statusError = signal<string | null>(null);
  protected readonly editingNote = signal(false);
  protected readonly noteDraft = signal('');
  protected readonly noteError = signal<string | null>(null);
  protected readonly actions = computed(() => {
    const d = this.detail();
    return d ? ACTIONS[d.coachingStatus] : [];
  });
  protected readonly accountAction = computed(() => {
    const d = this.detail();
    return d ? ACCOUNT_ACTIONS[d.account.state] : null;
  });
  protected readonly accountMessage = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly fullDate = formatVnFullDate;

  protected readonly sections = REGISTRATION_SECTIONS;
  protected readonly coachingLabel = COACHING_STATUS_LABEL;
  protected readonly coachingTone = COACHING_STATUS_TONE;
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly accountTone = ACCOUNT_STATUS_TONE;
  protected readonly initials = initialsOf;
  protected readonly formatDay = formatVnDay;
  protected readonly formatDayTime = formatVnDayTime;

  ngOnInit(): void {
    // Câu báo do form Tạo hồ sơ chuyển sang (lưu được nhưng chưa gửi được thông tin đăng nhập)
    const notice = (history.state as { notice?: unknown } | null)?.notice;
    if (typeof notice === 'string') this.accountMessage.set({ ok: false, text: notice });
    void this.load();
  }

  protected async load(): Promise<void> {
    this.conflict.set(false);
    try {
      this.detail.set(await firstValueFrom(this.api.get(this.id())));
      this.loadFailed.set(false);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected openPanel(action: StatusAction): void {
    this.statusError.set(null);
    this.pending.set(action);
  }

  protected closePanel(): void {
    this.pending.set(null);
    this.statusError.set(null);
  }

  // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-006 @BR-f01-p1-tai-khoan-hoc-vien-017
  protected async runAccountAction(action: AccountAction): Promise<void> {
    const d = this.detail()!;
    this.busy.set(true);
    this.accountMessage.set(null);
    try {
      const request = action.kind === 'reset' ? this.api.sendResetLink(d.id) : this.api.sendCredentials(d.id);
      this.detail.set(await firstValueFrom(request));
      this.accountMessage.set({ ok: true, text: action.done });
    } catch (error) {
      this.accountMessage.set({
        ok: false,
        text: error instanceof HttpErrorResponse && typeof error.error?.title === 'string'
          ? error.error.title
          : 'Chưa gửi được email. Vui lòng thử lại sau.',
      });
      // 409 (đã kích hoạt / chưa kích hoạt): trạng thái đã đổi ở nơi khác → tải lại để nút đúng trạng thái
      if (error instanceof HttpErrorResponse && error.status === 409) await this.load();
    } finally {
      this.busy.set(false);
    }
  }

  // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-020
  protected async changeStatus(to: CoachingStatus, reason: string): Promise<void> {
    const d = this.detail()!;
    const saved = await this.save(this.api.changeStatus(d.id, to, reason, d.version), (m) => this.statusError.set(m));
    if (saved) this.closePanel();
  }

  protected startNote(d: StudentDetail): void {
    this.noteDraft.set(d.internalNote ?? '');
    this.noteError.set(null);
    this.editingNote.set(true);
  }

  protected cancelNote(): void {
    this.noteError.set(null);
    this.editingNote.set(false);
  }

  // @BR-f02-ho-so-hoc-vien-014
  protected async saveNote(): Promise<void> {
    const d = this.detail()!;
    const saved = await this.save(this.api.updateNote(d.id, this.noteDraft(), d.version), (m) => this.noteError.set(m));
    if (saved) this.editingNote.set(false);
  }

  protected text(d: StudentDetail, key: string): string {
    const value = d.registration[key];
    return typeof value === 'string' && value ? value : 'Chưa có';
  }

  protected display(d: StudentDetail, field: FieldDef): string {
    const value = d.registration[field.key];
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'number') return field.unit ? `${formatNumber(value)} ${field.unit}` : formatNumber(value);
    if (field.type === 'date') return formatDateOnly(value);
    return value;
  }

  protected isPhoto(field: FieldDef): boolean {
    return field.key.startsWith('photo');
  }

  protected label(code: string): string {
    return FIELD_LABEL[code] ?? code;
  }

  /** Gửi lệnh sửa; 409 → yêu cầu tải lại (@BR-f02-ho-so-hoc-vien-015); lỗi khác → câu báo của server. */
  private async save(request: Observable<StudentDetail>, onError: (message: string) => void): Promise<boolean> {
    this.busy.set(true);
    try {
      this.detail.set(await firstValueFrom(request));
      return true;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        this.conflict.set(true); // giữ nguyên khung lý do / ô ghi chú để không mất chữ đã gõ; Tải lại chỉ làm mới dữ liệu + version
      } else {
        onError(error instanceof HttpErrorResponse && typeof error.error?.title === 'string'
          ? error.error.title
          : 'Không lưu được. Vui lòng thử lại.');
      }
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
