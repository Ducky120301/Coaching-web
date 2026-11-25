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
