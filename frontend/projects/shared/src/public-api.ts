// @ADR-f00-codebase-007: thư viện `shared` được dùng trực tiếp từ source qua tsconfig path
// `@coaching/shared` -> projects/shared/src/public-api.ts (không build dist/shared).
export * from './lib/config/app-config';
