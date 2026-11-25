import { HttpErrorResponse } from '@angular/common/http';

export const CONNECTION_ERROR = 'Không kết nối được hệ thống. Vui lòng thử lại.';
export const GENERIC_ERROR = 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';

/** Mã lỗi nghiệp vụ trong ProblemDetails (`code`). */
export function problemCode(error: unknown): string | null {
  const code = error instanceof HttpErrorResponse ? (error.error as { code?: unknown } | null)?.code : null;
  return typeof code === 'string' ? code : null;
}

/** Màn tài khoản: lỗi nghiệp vụ (400/401/429) hiện đúng câu của API; mạng → câu kết nối; còn lại câu chung. */
export function accountErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return CONNECTION_ERROR;
    const title = (error.error as { title?: unknown } | null)?.title;
    if ([400, 401, 429].includes(error.status) && typeof title === 'string') return title;
  }
  return GENERIC_ERROR;
}
