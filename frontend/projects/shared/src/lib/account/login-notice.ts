import { Injectable } from '@angular/core';

export interface LoginNotice {
  tone: 'ok' | 'error';
  text: string;
}

/** Câu báo mang từ màn khác sang trang đăng nhập (đặt lại xong, phiên kích hoạt hết hạn); đọc một lần. */
@Injectable({ providedIn: 'root' })
export class LoginNoticeStore {
  private notice: LoginNotice | null = null;

  set(notice: LoginNotice): void {
    this.notice = notice;
  }

  take(): LoginNotice | null {
    const notice = this.notice;
    this.notice = null;
    return notice;
  }
}
