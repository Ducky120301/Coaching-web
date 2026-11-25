import { describe, expect, it } from 'vitest';
import { initialsOf } from './initials';

describe('initialsOf', () => {
  it('takes first letters of the first and last word', () => {
    expect(initialsOf('Phạm Hoàng Quân')).toBe('PQ');
    expect(initialsOf('  lan ')).toBe('L');
    expect(initialsOf('')).toBe('?');
  });
});
