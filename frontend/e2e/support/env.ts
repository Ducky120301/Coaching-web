export const ADMIN_URL = process.env.E2E_ADMIN_URL ?? 'http://localhost:4200';
export const CLIENT_URL = process.env.E2E_CLIENT_URL ?? 'http://localhost:4300';
export const ADMIN = { email: process.env.ADMIN_EMAIL ?? 'admin@coaching.local', password: process.env.ADMIN_PASSWORD ?? 'Admin@12345' };
export const STUDENT = { email: process.env.DEMO_EMAIL ?? 'hocvien.demo@coaching.local', password: process.env.DEMO_PASSWORD ?? 'Demo@12345' };
