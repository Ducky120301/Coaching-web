import { BadgeTone } from '@coaching/shared';
import { AccountStatus, CoachingStatus } from './student.models';

export const COACHING_STATUS_LABEL: Record<CoachingStatus, string> = {
  Active: 'Đang coaching',
  Paused: 'Tạm dừng',
  Ended: 'Đã kết thúc',
};

export const ACCOUNT_STATUS_LABEL: Record<AccountStatus, string> = {
  PendingActivation: 'Chờ kích hoạt',
  Activated: 'Đã kích hoạt',
};

export const COACHING_STATUS_TONE: Record<CoachingStatus, BadgeTone> = { Active: 'ok', Paused: 'paused', Ended: 'ended' };

export const ACCOUNT_STATUS_TONE: Record<AccountStatus, BadgeTone> = { PendingActivation: 'warn', Activated: 'ok' };
