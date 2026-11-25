import { Injectable, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export interface ActivationTicket {
  token: string;
  expiresAt: string;
}

// @ADR-f01-p1-tai-khoan-hoc-vien-010: mã kích hoạt chỉ giữ trong bộ nhớ của FE — tải lại /kich-hoat là mất,
// học viên đăng nhập lại bằng mật khẩu tạm.
@Injectable({ providedIn: 'root' })
export class ActivationStore {
  private readonly ticket = signal<ActivationTicket | null>(null);
  readonly current = this.ticket.asReadonly();

  set(ticket: ActivationTicket): void {
    this.ticket.set(ticket);
  }

  clear(): void {
    this.ticket.set(null);
  }
}

/** Vào /kich-hoat mà không có mã kích hoạt → về /login. */
export const activationGuard: CanActivateFn = () =>
  inject(ActivationStore).current() ? true : inject(Router).createUrlTree(['/login']);
