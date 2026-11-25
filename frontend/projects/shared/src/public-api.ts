// @ADR-f00-codebase-007: thư viện `shared` được dùng trực tiếp từ source qua tsconfig path
// `@coaching/shared` -> projects/shared/src/public-api.ts (không build dist/shared).
export * from './lib/config/app-config';
export * from './lib/auth/client-app';
export * from './lib/auth/models';
export * from './lib/auth/skip-auth';
export * from './lib/auth/token-store';
export * from './lib/auth/refresh-coordinator';
export * from './lib/auth/auth.service';
export * from './lib/auth/auth.interceptor';
export * from './lib/auth/auth.guard';
export * from './lib/auth/return-url';
export * from './lib/auth/provide-auth';
export * from './lib/auth/activation-store';
export * from './lib/account/login-notice';
export * from './lib/login/login-page.component';
export * from './lib/login/auth-frame.component';
export * from './lib/format/vn-date';
export * from './lib/ui/icons';
export * from './lib/ui/user-menu.component';
export * from './lib/format/initials';
export * from './lib/ui/status-badge.component';
export * from './lib/account/account-errors';
export * from './lib/account/password-checklist.component';
export * from './lib/account/activate-page.component';
export * from './lib/account/forgot-password-page.component';
export * from './lib/account/reset-password-page.component';
