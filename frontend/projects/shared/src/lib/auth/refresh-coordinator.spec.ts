import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CLIENT_APP } from './client-app';
import { RefreshCoordinator } from './refresh-coordinator';

describe('RefreshCoordinator', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [{ provide: CLIENT_APP, useValue: 'admin' }] });
  });

  // @BR-f00-codebase-010: nhiều thao tác cùng lúc → chỉ gia hạn một lần
  it('gộp các lần gọi đồng thời thành một lần chạy', async () => {
    const coordinator = TestBed.inject(RefreshCoordinator);
    let release!: (v: string) => void;
    const task = vi.fn(() => new Promise<string>((resolve) => (release = resolve)));

    const a = coordinator.run(task);
    const b = coordinator.run(task);
    await Promise.resolve();
    release('token-moi');

    await expect(a).resolves.toBe('token-moi');
    await expect(b).resolves.toBe('token-moi');
    expect(task).toHaveBeenCalledTimes(1);
  });

  it('cho phép chạy lại sau khi lần trước xong', async () => {
    const coordinator = TestBed.inject(RefreshCoordinator);
    const task = vi.fn().mockResolvedValueOnce('t1').mockResolvedValueOnce('t2');
    await coordinator.run(task);
    await coordinator.run(task);
    expect(task).toHaveBeenCalledTimes(2);
  });

  // @BR-f00-codebase-014
  it('báo cho tab khác khi đăng xuất', async () => {
    const coordinator = TestBed.inject(RefreshCoordinator);
    const other = new BroadcastChannel('dbx-auth-admin');
    const received = new Promise<unknown>((resolve) => other.addEventListener('message', (e) => resolve(e.data)));
    coordinator.broadcastLogout();
    await expect(received).resolves.toBe('logout');
    other.close();
  });
});
