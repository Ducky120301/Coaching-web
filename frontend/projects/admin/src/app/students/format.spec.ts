import { describe, expect, it } from 'vitest';
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime, formatVnFullDate, formatVnTime } from './format';

describe('format', () => {
  it('formats instants in Vietnam time', () => {
    expect(formatVnDay('2026-11-23T02:00:00+00:00')).toBe('23/11');
    expect(formatVnDay('2026-11-24T23:30:00Z')).toBe('25/11');
    expect(formatVnDayTime('2026-11-25T02:00:00+00:00')).toBe('25/11 09:00');
  });

  it('formats full dates and times in Vietnam time', () => {
    expect(formatVnFullDate('2026-10-13T01:12:00+00:00')).toBe('13/10/2026');
    expect(formatVnFullDate('2026-12-31T18:00:00Z')).toBe('01/01/2027');
    expect(formatVnTime('2026-11-25T02:05:00+00:00')).toBe('09:05');
  });

  it('formats dates and numbers the Vietnamese way', () => {
    expect(formatDateOnly('2026-11-25')).toBe('25/11/2026');
    expect(formatNumber(72.5)).toBe('72,5');
    expect(formatNumber(3500000)).toBe('3.500.000');
  });
});
