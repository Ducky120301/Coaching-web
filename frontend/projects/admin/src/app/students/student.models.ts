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

/** F01-P1: trạng thái tài khoản tính ở server theo giờ hiện tại (@BR-f01-p1-tai-khoan-hoc-vien-026). */
export type AccountState = 'NotSent' | 'Sent' | 'TempPasswordExpired' | 'Activated';
export type AccountEventType = 'CredentialsSent' | 'CredentialsResent' | 'EmailChanged' | 'ResetLinkSent' | 'Activated';

export interface AccountEvent {
  type: AccountEventType;
  at: string;
  fromEmail: string | null;
  toEmail: string | null;
}

/** `account` của StudentDetailDto — không có mật khẩu tạm hay token (@BR-f01-p1-tai-khoan-hoc-vien-025). History mới nhất trước. */
export interface AccountInfo {
  state: AccountState;
  credentialsSentAt: string | null;
  tempPasswordExpiresAt: string | null;
  activatedAt: string | null;
  history: AccountEvent[];
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
  account: AccountInfo;
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
