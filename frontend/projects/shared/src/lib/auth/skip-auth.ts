import { HttpContextToken } from '@angular/common/http';

/** Request không gắn Bearer và không tự gia hạn (login/refresh/logout). */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
