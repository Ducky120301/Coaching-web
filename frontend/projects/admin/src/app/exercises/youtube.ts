/** Regex nguyên văn prototype AdThuVien (`yid`). @ADR-f03-thu-vien-bai-tap-005 */
const VIDEO_ID = /(?:shorts\/|v=|youtu\.be\/|embed\/)([\w-]{11})/;

export function youTubeId(url: string | null | undefined): string | null {
  const m = VIDEO_ID.exec(url ?? '');
  return m ? m[1] : null;
}

export function isShort(url: string | null | undefined): boolean {
  return /\/shorts\//.test(url ?? '');
}

/** Chỉ nhúng qua youtube-nocookie với mã đã rút ([\w-]{11}) — không bao giờ nhúng URL coach nhập. */
export function embedUrl(id: string): string {
  return `https://www.youtube-nocookie.com/embed/${id}?rel=0&modestbranding=1&playsinline=1`;
}
