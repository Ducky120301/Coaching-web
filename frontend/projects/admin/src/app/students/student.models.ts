export type CoachingStatus = 'Active' | 'Paused' | 'Ended';
export type AccountStatus = 'PendingActivation' | 'Activated';

/** GET /api/admin/students — mỗi dòng có tên, email, trạng thái coaching, trạng thái tài khoản. */
export interface StudentListItem {
  id: string;
  fullName: string;
  email: string;
  coachingStatus: CoachingStatus;
  accountStatus: AccountStatus;
  createdAt: string;
}

export type RegistrationValue = string | number | null;

/** `registration` của API: object phẳng theo mã mục (pack, price, transferDate, weightKg, …). */
export type Registration = Record<string, RegistrationValue>;

export interface StatusChange {
  from: CoachingStatus;
  to: CoachingStatus;
  actor: string;
  at: string;
  reason: string;
}

/** GET /api/admin/students/{id}. */
export interface StudentDetail {
  id: string;
  coachId: string;
  fullName: string;
  email: string;
  accountStatus: AccountStatus;
  coachingStatus: CoachingStatus;
  createdAt: string;
  registration: Registration;
  missingRequired: string[];
  updatedAt: string | null;
  updatedBy: string | null;
  version: string;
  internalNote: string | null;
  internalNoteUpdatedAt: string | null;
  statusHistory: StatusChange[];
}

export interface CreateStudentBody {
  fullName: string;
  email: string;
  registration: Registration;
  /** Tùy chọn — server lưu cùng transaction tạo hồ sơ. */
  internalNote?: string;
}

export interface UpdateStudentBody {
  fullName: string;
  registration: Registration;
  version: string;
}
