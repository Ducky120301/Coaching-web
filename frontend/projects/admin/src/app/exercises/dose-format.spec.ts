import { describe, expect, it } from 'vitest';
import { formatCount, formatRange, formatRest, parseRange, parseWhole } from './dose-format';

// @ADR-f03-thu-vien-bai-tap-006: FE nhận chữ "10–12"/"10-12"/"10", gửi min/max; hiển thị "10–12", "90s", trống "—"
describe('parseRange', () => {
  it.each([
    ['10', { min: 10, max: 10 }],
    ['10–12', { min: 10, max: 12 }],
    ['10-12', { min: 10, max: 12 }],
    [' 10 — 12 ', { min: 10, max: 12 }],
    ['0–1', { min: 0, max: 1 }],
    ['', { min: null, max: null }],
    ['   ', { min: null, max: null }],
  ])('reads "%s"', (text, expected) => {
    expect(parseRange(text)).toEqual(expected);
  });

  it.each(['abc', '10–', '–12', '1.5', '10–12–14', '1000'])('rejects "%s" as invalid', (text) => {
    expect(parseRange(text)).toBe('invalid');
  });

  it('leaves the range rule to the API (BR-f03-thu-vien-bai-tap-010): "12–10" is parsed as typed', () => {
    expect(parseRange('12–10')).toEqual({ min: 12, max: 10 });
  });
});

describe('parseWhole', () => {
  it.each([['3', 3], [' 90 ', 90], ['', null], ['  ', null]])('reads "%s"', (text, expected) => {
    expect(parseWhole(text)).toBe(expected);
  });

  it.each(['abc', '1.5', '-3', '3 hiệp'])('rejects "%s" as invalid', (text) => {
    expect(parseWhole(text)).toBe('invalid');
  });
});

describe('format', () => {
  it('formats a range with an en dash, a single value without, empty as —', () => {
    expect(formatRange(10, 12)).toBe('10–12');
    expect(formatRange(10, 10)).toBe('10');
    expect(formatRange(1, null)).toBe('1');
    expect(formatRange(null, null)).toBe('—');
    expect(formatRange(null, null, '')).toBe('');
  });

  it('formats rest as seconds and sets as a plain number', () => {
    expect(formatRest(90)).toBe('90s');
    expect(formatRest(null)).toBe('—');
    expect(formatCount(3)).toBe('3');
    expect(formatCount(null)).toBe('—');
  });
});
