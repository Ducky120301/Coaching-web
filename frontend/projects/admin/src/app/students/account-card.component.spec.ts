import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { AccountCardComponent, accountEventText, accountStateText } from './account-card.component';
import { AccountInfo } from './student.models';

const base: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };

describe('account card', () => {
  // @BR-f01-p1-tai-khoan-hoc-vien-026: câu trạng thái theo living docs
  it('states the account the way the spec says', () => {
    expect(accountStateText(base)).toBe('Chưa gửi thông tin đăng nhập');
    expect(accountStateText({ ...base, state: 'Sent', credentialsSentAt: '2026-11-01T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-04T02:00:00+00:00' }))
      .toBe('Đã gửi lúc 01/11 09:00, mật khẩu tạm còn hạn đến 04/11 09:00');
    expect(accountStateText({ ...base, state: 'TempPasswordExpired', credentialsSentAt: '2026-11-01T02:00:00+00:00' })).toBe('Mật khẩu tạm đã hết hạn');
    expect(accountStateText({ ...base, state: 'Activated', activatedAt: '2026-10-13T01:12:00+00:00' })).toBe('Đã kích hoạt ngày 13/10/2026');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-027: một câu cho mỗi loại sự kiện (tech-design / prototype AdHoSo)
  it('writes one sentence per history event type', () => {
    const at = '2026-10-12T02:05:00+00:00';
    expect(accountEventText({ type: 'CredentialsSent', at, fromEmail: null, toEmail: null })).toBe('Gửi thông tin đăng nhập lần đầu.');
    expect(accountEventText({ type: 'CredentialsResent', at, fromEmail: null, toEmail: null })).toBe('Gửi lại thông tin đăng nhập. Mật khẩu tạm cũ bị huỷ.');
    expect(accountEventText({ type: 'EmailChanged', at, fromEmail: 'quan@gmial.com', toEmail: 'quan@gmail.com' })).toBe('Sửa email từ quan@gmial.com sang quan@gmail.com.');
    expect(accountEventText({ type: 'ResetLinkSent', at, fromEmail: null, toEmail: null })).toBe('Gửi link đặt lại mật khẩu.');
    expect(accountEventText({ type: 'Activated', at, fromEmail: null, toEmail: null })).toBe('Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.');
  });

  it('renders the state line, the history (newest first as given) and the closing note', () => {
    const fixture = TestBed.createComponent(AccountCardComponent);
    fixture.componentRef.setInput('account', {
      ...base, state: 'Activated', activatedAt: '2026-10-13T01:12:00+00:00',
      history: [
        { type: 'Activated', at: '2026-10-13T01:12:00+00:00', fromEmail: null, toEmail: null },
        { type: 'CredentialsSent', at: '2026-10-12T02:05:00+00:00', fromEmail: null, toEmail: null },
      ],
    } satisfies AccountInfo);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Tài khoản đăng nhập');
    expect(el.querySelector('.state')?.textContent?.trim()).toBe('Đã kích hoạt ngày 13/10/2026');
    const rows = [...el.querySelectorAll('ol li')].map((li) => li.textContent?.replace(/\s+/g, ' ').trim());
    expect(rows).toEqual(['13/10 08:12 Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.', '12/10 09:05 Gửi thông tin đăng nhập lần đầu.']);
    expect(el.querySelector('.foot')?.textContent?.trim()).toBe('Mật khẩu riêng do học viên quản lý. Coach chỉ gửi được link đặt lại mật khẩu qua email.');
  });
});
