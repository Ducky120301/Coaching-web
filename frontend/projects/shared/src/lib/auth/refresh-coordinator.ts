import { Injectable, OnDestroy, inject } from '@angular/core';
import { CLIENT_APP } from './client-app';

// @ADR-f00-codebase-002 @BR-f00-codebase-010 @BR-f00-codebase-014
@Injectable({ providedIn: 'root' })
export class RefreshCoordinator implements OnDestroy {
  private readonly app = inject(CLIENT_APP);
  private readonly channel: BroadcastChannel | null =
    typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(`dbx-auth-${this.app}`);
  private inFlight: Promise<string> | null = null;

  /** Single-flight trong tab + khóa giữa các tab (Web Locks). Task phải đọc refresh token mới nhất bên trong. */
  run(task: () => Promise<string>): Promise<string> {
    this.inFlight ??= this.withCrossTabLock(task).finally(() => (this.inFlight = null));
    return this.inFlight;
  }

  broadcastLogout(): void {
    this.channel?.postMessage('logout');
  }

  onLoggedOutElsewhere(handler: () => void): void {
    this.channel?.addEventListener('message', (event) => {
      if (event.data === 'logout') handler();
    });
  }

  ngOnDestroy(): void {
    this.channel?.close();
  }

  private withCrossTabLock(task: () => Promise<string>): Promise<string> {
    const locks = typeof navigator === 'undefined' ? undefined : navigator.locks;
    return locks ? locks.request(`dbx-refresh-${this.app}`, () => task()) : task();
  }
}
