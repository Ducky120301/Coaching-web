// @BR-f00-codebase-015: chỉ quay về đường dẫn thuộc cùng web
export function safeReturnUrl(raw: string | null | undefined): string {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//') || raw.startsWith('/\\') || raw.includes('://')) {
    return '/';
  }
  return raw;
}
