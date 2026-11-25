import { describe, expect, it } from 'vitest';
import {
  FORM_SECTIONS, REGISTRATION_FIELDS, parseDecimal, parseInteger, toFormValues, toPayload,
} from './registration-fields';
import { StudentDetail } from './student.models';

describe('registration fields', () => {
  it('follows the prototype groups plus Lịch rảnh and Dụng cụ, without Gán giáo án', () => {
    expect(FORM_SECTIONS.map((s) => s.title)).toEqual([
      'Tài khoản và gói', 'Số đo cơ thể hiện tại', 'Tập luyện hiện tại', 'Ăn uống, sinh hoạt và môi trường sống',
      'Chương trình tập sắp tới', 'Kỳ vọng và cảm nhận', 'Chỉ coach thấy',
    ]);
    const training = FORM_SECTIONS[2].fields.map((f) => f.label);
    expect(training).toContain('Lịch rảnh');
    expect(training).toContain('Dụng cụ');
    expect(FORM_SECTIONS.flatMap((s) => s.fields).some((f) => f.label === 'Gán giáo án')).toBe(false);
    expect(REGISTRATION_FIELDS).toHaveLength(44);
    expect(FORM_SECTIONS.flatMap((s) => s.fields).filter((f) => f.required)).toHaveLength(30);
  });

  // @BR-f02-ho-so-hoc-vien-008: dấu phẩy thập phân
  it('parses decimal comma and grouped integers', () => {
    expect(parseDecimal('72,5')).toBe(72.5);
    expect(parseDecimal('250.1')).toBe(250.1);
    expect(parseDecimal('abc')).toBeNull();
    expect(parseInteger('3.500.000')).toBe(3500000);
    expect(parseInteger('28')).toBe(28);
    expect(parseInteger('2,5')).toBeNull();
    expect(parseInteger('3500000')).toBe(3500000);
    expect(parseInteger('3.5')).toBeNull();
    expect(parseInteger('35.00')).toBeNull();
  });

  it('builds the API payload and reports invalid numbers', () => {
    const { payload, errors } = toPayload({ fullName: ' Lan ', email: 'lan@gmail.com', weightKg: '72,5', age: 'abc', pack: 'Pack 2', internalNote: ' Gối phải ' });
    expect(payload.fullName).toBe('Lan');
    expect(payload.registration['weightKg']).toBe(72.5);
    expect(payload.registration['pack']).toBe('Pack 2');
    expect(payload.registration['trainingGoal']).toBeNull();
    expect(payload.internalNote).toBe('Gối phải');
    expect(errors).toEqual({ age: 'Nhập số hợp lệ' });
  });

  it('shows stored numbers with a decimal comma', () => {
    const values = toFormValues({ fullName: 'Lan', email: 'lan@gmail.com', internalNote: null,
      registration: { weightKg: 72.5, age: 28, transferDate: '2026-11-25' } } as unknown as StudentDetail);
    expect(values['weightKg']).toBe('72,5');
    expect(values['age']).toBe('28');
    expect(values['transferDate']).toBe('2026-11-25');
    expect(values['internalNote']).toBe('');
  });
});
