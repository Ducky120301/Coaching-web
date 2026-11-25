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
