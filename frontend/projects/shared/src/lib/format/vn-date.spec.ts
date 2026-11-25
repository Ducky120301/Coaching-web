import { describe, expect, it } from 'vitest';
import { formatVnDate } from './vn-date';

describe('formatVnDate', () => {
  it('có năm: "Thứ Hai, 23/11/2026"', () => {
    expect(formatVnDate(new Date(2026, 10, 23), true)).toBe('Thứ Hai, 23/11/2026');
  });
  it('không năm: "Thứ Hai, 30/11"', () => {
    expect(formatVnDate(new Date(2026, 10, 30), false)).toBe('Thứ Hai, 30/11');
  });
  it('Chủ nhật', () => {
    expect(formatVnDate(new Date(2026, 10, 29), false)).toBe('Chủ nhật, 29/11');
  });
});
