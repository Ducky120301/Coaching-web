import { describe, expect, it, vi } from 'vitest';
import { loadAppConfig } from './app-config';

describe('loadAppConfig', () => {
  it('đọc apiBaseUrl từ /config.json', async () => {
    const fetchFn = vi.fn().mockResolvedValue(new Response(JSON.stringify({ apiBaseUrl: 'https://api.test' })));
    await expect(loadAppConfig(fetchFn)).resolves.toEqual({ apiBaseUrl: 'https://api.test' });
    expect(fetchFn).toHaveBeenCalledWith('/config.json', { cache: 'no-store' });
  });

  it('báo lỗi khi không tải được cấu hình', async () => {
    const fetchFn = vi.fn().mockResolvedValue(new Response('', { status: 404 }));
    await expect(loadAppConfig(fetchFn)).rejects.toThrow('Không tải được /config.json');
  });

  it('bỏ dấu / cuối apiBaseUrl', async () => {
    const fetchFn = vi.fn().mockResolvedValue(new Response(JSON.stringify({ apiBaseUrl: 'https://api.test/' })));
    await expect(loadAppConfig(fetchFn)).resolves.toEqual({ apiBaseUrl: 'https://api.test' });
  });
});
