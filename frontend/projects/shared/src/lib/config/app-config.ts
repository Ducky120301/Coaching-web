import { InjectionToken } from '@angular/core';

// @ADR-f00-codebase-006: địa chỉ API đọc lúc chạy, 1 bản build cho mọi môi trường
export interface AppConfig {
  /** '' = cùng origin (compose/nginx proxy /api); URL đầy đủ khi API ở origin khác (Render). */
  apiBaseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export async function loadAppConfig(fetchFn: typeof fetch = fetch): Promise<AppConfig> {
  const response = await fetchFn('/config.json', { cache: 'no-store' });
  if (!response.ok) {
    throw new Error(`Không tải được /config.json (HTTP ${response.status})`);
  }
  const raw = (await response.json()) as Partial<AppConfig>;
  return { apiBaseUrl: (raw.apiBaseUrl ?? '').replace(/\/+$/, '') };
}
