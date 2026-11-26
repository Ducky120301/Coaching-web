/** @ADR-f03-thu-vien-bai-tap-006: liều nhập dạng chữ ở FE, gửi API dạng số; khoảng hợp lệ do Domain kiểm (BR-009–012). */
export interface DoseRange {
  min: number | null;
  max: number | null;
}

const RANGE = /^(\d{1,3})(?:\s*[-‐–—]\s*(\d{1,3}))?$/;
const WHOLE = /^\d{1,4}$/;

/** "10" → 10–10; "10–12" / "10-12" → 10–12; trống → {null, null}; sai định dạng → 'invalid'. */
export function parseRange(text: string): DoseRange | 'invalid' {
  const t = text.trim();
  if (!t) return { min: null, max: null };
  const m = RANGE.exec(t);
  if (!m) return 'invalid';
  const min = Number(m[1]);
  return { min, max: m[2] === undefined ? min : Number(m[2]) };
}

/** Số nguyên không dấu (Sets, Nghỉ giây); trống → null; khác → 'invalid'. */
export function parseWhole(text: string): number | null | 'invalid' {
  const t = text.trim();
  if (!t) return null;
  return WHOLE.test(t) ? Number(t) : 'invalid';
}

/** 10–12 → "10–12"; 10–10 → "10"; trống → `empty` ("—" khi hiển thị, "" khi đổ vào ô nhập). */
export function formatRange(min: number | null, max: number | null, empty = '—'): string {
  if (min === null && max === null) return empty;
  if (min === null) return `${max}`;
  if (max === null || max === min) return `${min}`;
  return `${min}–${max}`;
}

export function formatCount(value: number | null): string {
  return value === null ? '—' : `${value}`;
}

/** 90 → "90s" như prototype. */
export function formatRest(seconds: number | null): string {
  return seconds === null ? '—' : `${seconds}s`;
}
