const WEEKDAYS = ['Chủ nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
const pad = (n: number) => String(n).padStart(2, '0');

export function formatVnDate(date: Date, withYear: boolean): string {
  const day = `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
  return `${WEEKDAYS[date.getDay()]}, ${withYear ? `${day}/${date.getFullYear()}` : day}`;
}
