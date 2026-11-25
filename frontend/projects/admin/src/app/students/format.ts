const pad = (n: number) => String(n).padStart(2, '0');
const VIETNAM_OFFSET_MS = 7 * 60 * 60 * 1000; // UTC+7 cố định, không giờ mùa hè

function vietnamParts(iso: string) {
  const d = new Date(Date.parse(iso) + VIETNAM_OFFSET_MS);
  return {
    day: pad(d.getUTCDate()), month: pad(d.getUTCMonth() + 1), year: String(d.getUTCFullYear()),
    hour: pad(d.getUTCHours()), minute: pad(d.getUTCMinutes()),
  };
}

/** "dd/MM/yyyy" theo giờ Việt Nam (vd. "Kích hoạt 13/10/2026"). */
export function formatVnFullDate(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month}/${p.year}`;
}

/** "HH:mm" theo giờ Việt Nam (vd. stNote "Gửi đến … lúc 09:05."). */
export function formatVnTime(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.hour}:${p.minute}`;
}

/** "dd/MM" theo giờ Việt Nam (vd. "Sửa lần cuối 23/11."). */
export function formatVnDay(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month}`;
}

/** "dd/MM HH:mm" theo giờ Việt Nam (dòng lịch sử, kiểu card "Tài khoản đăng nhập" của prototype). */
export function formatVnDayTime(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month} ${p.hour}:${p.minute}`;
}

/** "2026-11-25" → "25/11/2026". */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split('-');
  return `${day}/${month}/${year}`;
}

const NUMBER = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });

/** 72.5 → "72,5"; 3500000 → "3.500.000". */
export function formatNumber(value: number): string {
  return NUMBER.format(value);
}
