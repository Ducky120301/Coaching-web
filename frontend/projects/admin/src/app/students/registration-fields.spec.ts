import { describe, expect, it } from 'vitest';
import {
  FIELD_LABEL, FORM_FIELDS, FORM_SECTIONS, REGISTRATION_FIELDS, parseDecimal, parseInteger, toFormValues, toPayload,
} from './registration-fields';
import { StudentDetail } from './student.models';

const REMOVED = ['availability', 'equipment', 'bodyFatPercent', 'bodyFatMethod', 'weightBeforeLiftingKg', 'heightThenCm',
  'photoFrontUrl', 'photoBackUrl', 'photoLeftUrl', 'photoRightUrl'];

describe('registration fields', () => {
  // @BR-f02-import-excel-001 @BR-f02-import-excel-003 @BR-f02-import-excel-004 @BR-f02-import-excel-005 @BR-f02-import-excel-006 @BR-f02-import-excel-007
  it('follows the Google Form: no Lịch rảnh / Dụng cụ, merged items, one full-body photo link, without Gán giáo án', () => {
    expect(FORM_SECTIONS.map((s) => s.title)).toEqual([
      'Tài khoản và gói', 'Số đo cơ thể hiện tại', 'Tập luyện hiện tại', 'Ăn uống, sinh hoạt và môi trường sống',
      'Chương trình tập sắp tới', 'Kỳ vọng và cảm nhận', 'Chỉ coach thấy',
    ]);
    const labels = FORM_FIELDS.map((f) => f.label);
    for (const gone of ['Lịch rảnh', 'Dụng cụ', 'Gán giáo án', 'Cách đo mỡ', 'Chiều cao khi đó', 'Ảnh mặt trước (link)']) {
      expect(labels).not.toContain(gone);
    }
    for (const key of REMOVED) expect(FORM_FIELDS.some((f) => f.key === key)).toBe(false);

    const byKey = Object.fromEntries(FORM_FIELDS.map((f) => [f.key, f]));
    expect(byKey['trainingYears']).toMatchObject({ label: 'Số năm tập luyện', type: 'text', required: true });
    expect(byKey['trainingYears'].options).toBeUndefined();
    expect(byKey['bodyFatAndMethod']).toMatchObject({ label: 'Mỡ cơ thể và cách đo', type: 'text', required: false });
    expect(byKey['weightHeightBeforeLifting'])
      .toMatchObject({ label: 'Cân nặng và chiều cao trước khi tập tạ', type: 'area', required: true, wide: true });
    expect(byKey['bodyPhotoUrl']).toMatchObject({ label: 'Link ảnh toàn thân', type: 'url', required: true, wide: true });
    expect(FORM_SECTIONS[1].fields.at(-1)?.key).toBe('bodyFatAndMethod');
    expect(FORM_SECTIONS[2].fields.map((f) => f.key)).toEqual([
      'trainingYears', 'weeklyTrainingTime', 'weightHeightBeforeLifting', 'trainingGoal', 'currentTraining', 'medicalHistory',
    ]);
    expect(FORM_SECTIONS[4].fields.map((f) => f.key)).toEqual(['sessionsPerWeek', 'programStyle', 'bodyPhotoUrl']);

    expect(REGISTRATION_FIELDS).toHaveLength(37);
    expect(FORM_FIELDS.filter((f) => f.required)).toHaveLength(26);
    expect(FIELD_LABEL['bodyPhotoUrl']).toBe('Link ảnh toàn thân');
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

  // @BR-f02-import-excel-003 @BR-f02-import-excel-004 @BR-f02-import-excel-005 @BR-f02-import-excel-006: chữ giữ nguyên văn (chỉ trim), không đổi sang số
  it('sends the merged text items verbatim and never sends removed keys', () => {
    const { payload, errors } = toPayload({
      trainingYears: '1', bodyFatAndMethod: '18% đo InBody', weightHeightBeforeLifting: '60kg, 1m70',
      bodyPhotoUrl: ' https://drive.google.com/open?id=abc ', availability: 'T3–T7 buổi tối',
    });
    expect(errors).toEqual({});
    expect(payload.registration['trainingYears']).toBe('1');
    expect(payload.registration['bodyFatAndMethod']).toBe('18% đo InBody');
    expect(payload.registration['weightHeightBeforeLifting']).toBe('60kg, 1m70');
    expect(payload.registration['bodyPhotoUrl']).toBe('https://drive.google.com/open?id=abc');
    for (const key of REMOVED) expect(key in payload.registration).toBe(false);
    expect(Object.keys(payload.registration)).toHaveLength(37);
  });

  it('shows stored numbers with a decimal comma', () => {
    const values = toFormValues({ fullName: 'Lan', email: 'lan@gmail.com', internalNote: null,
      registration: { weightKg: 72.5, age: 28, transferDate: '2026-11-25', trainingYears: '2 năm' } } as unknown as StudentDetail);
    expect(values['weightKg']).toBe('72,5');
    expect(values['age']).toBe('28');
    expect(values['transferDate']).toBe('2026-11-25');
    expect(values['trainingYears']).toBe('2 năm');
    expect(values['internalNote']).toBe('');
  });
});
