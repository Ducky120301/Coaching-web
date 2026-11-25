/** "Phạm Hoàng Quân" → "PQ": chữ đầu của từ đầu + từ cuối (cùng luật với UserMenu, theo prototype). */
export function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  const picked = words.length === 1 ? [words[0]] : [words[0], words[words.length - 1]];
  return picked.map((w) => w[0]).join('').toUpperCase();
}
