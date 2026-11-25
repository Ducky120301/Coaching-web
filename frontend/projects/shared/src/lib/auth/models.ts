export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
}

/** @ADR-f01-p1-tai-khoan-hoc-vien-002: đăng nhập bằng mật khẩu tạm còn hạn → mã kích hoạt, không có phiên. */
export interface ActivationChallenge {
  activationRequired: true;
  activationToken: string;
  activationTokenExpiresAt: string;
}

export type LoginResult = { kind: 'session' } | { kind: 'activation'; token: string; expiresAt: string };
