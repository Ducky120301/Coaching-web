import { Registration, RegistrationValue, StudentDetail } from './student.models';

/** `url`: ô 1 dòng nhận link (server kiểm http/https ≤ 2048, @BR-f02-import-excel-006). */
export type FieldType = 'text' | 'decimal' | 'integer' | 'date' | 'area' | 'select' | 'url';

export interface FieldDef {
  key: string;
  label: string;
  type: FieldType;
  required: boolean;
  unit?: string;
  placeholder?: string;
  note?: string;
  /** Chiếm cả hàng lưới (prototype w: 2). */
  wide?: boolean;
  rows?: number;
  options?: readonly string[];
}

export interface SectionDef {
  id: string;
  title: string;
  hint?: string;
  fields: readonly FieldDef[];
}

/**
 * Form đăng ký theo page AdTaoHoSo (thứ tự, nhãn, placeholder, đơn vị nguyên văn prototype — `SECS` đã sửa theo Google Form,
 * @ADR-f02-import-excel-001); bỏ "Gán giáo án" (F05). Danh sách lựa chọn là bản sao để hiển thị — server kiểm lại (RegistrationOptions).
 */
export const FORM_SECTIONS: readonly SectionDef[] = [
  {
    id: 'tk', title: 'Tài khoản và gói', hint: 'Thông tin để tạo tài khoản và ghi nhận thanh toán.',
    fields: [
      { key: 'fullName', label: 'Họ và tên', type: 'text', required: true, placeholder: 'VD: Phạm Hoàng Quân' },
      { key: 'email', label: 'Email đăng nhập', type: 'text', required: true, placeholder: 'ten@gmail.com', note: 'Hệ thống gửi mật khẩu tạm về email này.' },
      { key: 'pack', label: 'Gói đã mua', type: 'select', required: true, options: ['Pack 1', 'Pack 2', 'Pack 3', 'Khác'] },
      { key: 'price', label: 'Giá', type: 'integer', required: true, unit: 'đ', placeholder: 'VD: 3.500.000' },
      { key: 'transferDate', label: 'Ngày chuyển khoản', type: 'date', required: true },
      { key: 'address', label: 'Địa chỉ', type: 'text', required: true, placeholder: 'Tỉnh / thành phố' },
      { key: 'age', label: 'Tuổi', type: 'integer', required: true, unit: 'tuổi' },
      { key: 'heightCm', label: 'Chiều cao', type: 'decimal', required: true, unit: 'cm' },
    ],
  },
  {
    id: 'so', title: 'Số đo cơ thể hiện tại', hint: 'Số đo ban đầu, dùng để so sánh khi tổng kết phase.',
    fields: [
      { key: 'weightKg', label: 'Cân nặng', type: 'decimal', required: true, unit: 'kg' },
      { key: 'chestCm', label: 'Vòng 1 (ngực)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'bellyCm', label: 'Vòng 2 (bụng)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'waistCm', label: 'Vòng eo (trên rốn 4 cm)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'hipCm', label: 'Vòng 3 (mông)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'rightArmCm', label: 'Tay phải (gồng biceps)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'leftArmCm', label: 'Tay trái (gồng biceps)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'rightThighCm', label: 'Đùi phải (chỗ to nhất)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'leftThighCm', label: 'Đùi trái (chỗ to nhất)', type: 'decimal', required: false, unit: 'cm' },
      // @BR-f02-import-excel-004 @BR-f02-import-excel-007: chữ tự do ≤ 200, không bắt buộc
      { key: 'bodyFatAndMethod', label: 'Mỡ cơ thể và cách đo', type: 'text', required: false, placeholder: 'VD: 18% đo InBody' },
    ],
  },
  {
    id: 'tap', title: 'Tập luyện hiện tại',
    fields: [
      // @BR-f02-import-excel-003: chữ tự do ≤ 200, giữ nguyên văn
      { key: 'trainingYears', label: 'Số năm tập luyện', type: 'text', required: true, placeholder: 'VD: 2 năm' },
      { key: 'weeklyTrainingTime', label: 'Thời gian dành cho tập mỗi tuần', type: 'text', required: true, placeholder: 'VD: 5 buổi, mỗi buổi 75 phút' },
      // @BR-f02-import-excel-005: đoạn văn ≤ 2000
      { key: 'weightHeightBeforeLifting', label: 'Cân nặng và chiều cao trước khi tập tạ', type: 'area', required: true, wide: true, rows: 2, placeholder: 'VD: 60 kg, 1m70' },
      { key: 'trainingGoal', label: 'Mục tiêu tập luyện', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'currentTraining', label: 'Chế độ tập hiện tại', type: 'area', required: false, wide: true, rows: 4, placeholder: 'Lịch chia nhóm cơ, bài, số set, rep…' },
      { key: 'medicalHistory', label: 'Tiền sử bệnh lý và chấn thương', type: 'area', required: true, wide: true, rows: 2, placeholder: 'Ghi "Không" nếu không có' },
    ],
  },
  {
    id: 'an', title: 'Ăn uống, sinh hoạt và môi trường sống',
    fields: [
      { key: 'foodBudgetReadiness', label: 'Sẵn sàng đầu tư cho thực phẩm chất lượng', type: 'select', required: true, wide: true,
        options: ['Sẵn sàng', 'Đang cân nhắc', 'Chưa sẵn sàng'] },
      { key: 'occupation', label: 'Nghề nghiệp', type: 'text', required: false, wide: true, placeholder: 'Giờ làm, ca kíp, mức stress, đi lại nhiều không…' },
      { key: 'activityLevel', label: 'Mức độ hoạt động', type: 'select', required: true,
        options: ['Ít vận động (ngồi nhiều)', 'Nhẹ', 'Vừa', 'Nhiều', 'Rất nhiều (lao động chân tay)'] },
      { key: 'stressLevel', label: 'Mức độ căng thẳng', type: 'select', required: true, options: ['Thấp', 'Vừa', 'Cao', 'Rất cao'] },
      { key: 'sleepDuration', label: 'Thời gian ngủ mỗi ngày', type: 'select', required: true,
        options: ['Dưới 5 tiếng', '5–6 tiếng', '6–7 tiếng', '7–8 tiếng', 'Trên 8 tiếng'] },
      { key: 'caffeine', label: 'Caffeine mỗi ngày', type: 'text', required: false, placeholder: 'VD: 2 ly cà phê sữa' },
      { key: 'supplements', label: 'Thực phẩm bổ sung đang dùng', type: 'area', required: false, wide: true, rows: 2 },
      { key: 'currentDiet', label: 'Chế độ dinh dưỡng hiện tại', type: 'area', required: true, wide: true, rows: 4, placeholder: 'Các bữa trong ngày, ăn gì, lượng bao nhiêu…' },
      { key: 'mealAndTrainingTimes', label: 'Giờ có thể ăn và giờ đi tập', type: 'area', required: true, wide: true, rows: 3, placeholder: 'VD: Ăn 7h, 12h, 19h. Tập 17h30–19h.' },
    ],
  },
  {
    id: 'ct', title: 'Chương trình tập sắp tới',
    fields: [
      { key: 'sessionsPerWeek', label: 'Số buổi tập mỗi tuần', type: 'select', required: true,
        options: ['2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi'] },
      { key: 'programStyle', label: 'Phong cách giáo án muốn theo', type: 'select', required: true,
        options: ['Push / Pull / Legs', 'Upper / Lower', 'Full body', 'Mỗi buổi một nhóm cơ', 'Để coach chọn', 'Khác'] },
      // @BR-f02-import-excel-006: 1 link http/https thay 4 link ảnh
      { key: 'bodyPhotoUrl', label: 'Link ảnh toàn thân', type: 'url', required: true, wide: true, placeholder: 'Link Google Drive' },
    ],
  },
  {
    id: 'kv', title: 'Kỳ vọng và cảm nhận',
    fields: [
      { key: 'expectations', label: 'Mong đợi gì từ coach', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'previousCoaching', label: 'Từng theo coach / online coaching chưa, trải nghiệm ra sao', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'investmentReason', label: 'Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này', type: 'area', required: true, wide: true, rows: 2 },
    ],
  },
  {
    id: 'nb', title: 'Chỉ coach thấy', hint: 'Học viên không thấy phần này.',
    fields: [
      { key: 'internalNote', label: 'Ghi chú nội bộ', type: 'area', required: false, wide: true, rows: 2, placeholder: 'VD: Tránh lunge sâu vì gối phải' },
    ],
  },
];

export const FORM_FIELDS: readonly FieldDef[] = FORM_SECTIONS.flatMap((s) => s.fields);

const NOT_REGISTRATION = new Set(['fullName', 'email', 'internalNote']);

/** 37 mục của `registration` (bỏ họ tên, email, ghi chú nội bộ). */
export const REGISTRATION_FIELDS: readonly FieldDef[] = FORM_FIELDS.filter((f) => !NOT_REGISTRATION.has(f.key));

/** Nhãn hiển thị theo mã mục (dùng cho "Mục bắt buộc còn thiếu"). */
export const FIELD_LABEL: Readonly<Record<string, string>> = Object.fromEntries(FORM_FIELDS.map((f) => [f.key, f.label]));

export type FormValues = Record<string, string>;

export interface FormPayload {
  fullName: string;
  email: string;
  registration: Registration;
  internalNote: string;
}

/** "72,5" → 72.5 (dấu phẩy thập phân). Không phải số → null. */
export function parseDecimal(text: string): number | null {
  const normalized = text.replace(/\s/g, '').replace(',', '.');
  return /^-?\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : null;
}

/** "3.500.000" → 3500000 (dấu chấm phân nhóm). Không phải số nguyên → null. */
export function parseInteger(text: string): number | null {
  const trimmed = text.trim();
  if (/^\d+$/.test(trimmed)) return Number(trimmed);
  // dấu chấm chỉ là phân nhóm nghìn: "3.5", "35.00" không hợp lệ
  return /^\d{1,3}(\.\d{3})+$/.test(trimmed) ? Number(trimmed.replace(/\./g, '')) : null;
}

export function toInputText(value: RegistrationValue | undefined, type: FieldType): string {
  if (value === null || value === undefined) return '';
  if (type === 'decimal' && typeof value === 'number') return String(value).replace('.', ',');
  return String(value);
}

/** Giá trị ô nhập → body API. Ô số sai định dạng → lỗi "Nhập số hợp lệ" (không gửi). Kiểm khoảng do server. */
export function toPayload(values: FormValues): { payload: FormPayload; errors: Record<string, string> } {
  const errors: Record<string, string> = {};
  const registration: Registration = {};
  for (const field of REGISTRATION_FIELDS) {
    const raw = (values[field.key] ?? '').trim();
    if (!raw) {
      registration[field.key] = null;
    } else if (field.type === 'decimal' || field.type === 'integer') {
      const parsed = field.type === 'decimal' ? parseDecimal(raw) : parseInteger(raw);
      if (parsed === null) errors[field.key] = 'Nhập số hợp lệ';
      registration[field.key] = parsed;
    } else {
      registration[field.key] = raw;
    }
  }
  return {
    payload: {
      fullName: (values['fullName'] ?? '').trim(),
      email: (values['email'] ?? '').trim(),
      registration,
      internalNote: (values['internalNote'] ?? '').trim(),
    },
    errors,
  };
}

export function toFormValues(detail: StudentDetail): FormValues {
  const values: FormValues = {
    fullName: detail.fullName,
    email: detail.email,
    internalNote: detail.internalNote ?? '',
  };
  for (const field of REGISTRATION_FIELDS) values[field.key] = toInputText(detail.registration[field.key], field.type);
  return values;
}
