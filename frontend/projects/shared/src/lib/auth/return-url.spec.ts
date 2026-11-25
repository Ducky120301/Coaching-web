import { describe, expect, it } from 'vitest';
import { safeReturnUrl } from './return-url';

// @BR-f00-codebase-015
describe('safeReturnUrl', () => {
  it('giữ đường dẫn trong cùng web', () => {
    expect(safeReturnUrl('/trang-chu?tu=e2e')).toBe('/trang-chu?tu=e2e');
  });
  it('cho phép URL tuyệt đối nằm trong query', () => {
    expect(safeReturnUrl('/trang-chu?next=https://y')).toBe('/trang-chu?next=https://y');
  });
  it.each([
    ['/	/evil.com'],
    ['https://evil.example.com'],
    ['//evil.example.com'],
    ['/\\evil.example.com'],
    ['javascript:alert(1)'],
    [''],
    [null],
    [undefined],
  ])('đưa %s về trang chủ', (raw) => {
    expect(safeReturnUrl(raw as string | null | undefined)).toBe('/');
  });
});
