import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL, CLIENT_URL } from '../support/env';
import { E2EWorld } from '../support/world';

interface CapturedMail { to: string; subject: string; textBody: string; sentAt: string; }

const CREDENTIALS_SUBJECT = 'Thông tin đăng nhập Duobexiuuu Coaching';
const RESET_SUBJECT = 'Đặt lại mật khẩu Duobexiuuu Coaching';
const FORGOT_MESSAGE = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';
const FULL_NAME = 'Phạm Hoàng Quân';
const OWN_PASSWORD = 'Coach2025';

/** Đủ 24 mục bắt buộc của `registration` (+ họ tên, email = 26), giá trị hợp lệ theo RegistrationForm (Google Form, @ADR-f02-import-excel-001). */
const FULL_REGISTRATION = {
  pack: 'Pack 2', price: 3500000, transferDate: '2026-01-15', address: 'Hà Nội', age: 28, heightCm: 170, weightKg: 72.5,
  trainingYears: '1–2 năm', weeklyTrainingTime: '5 buổi, mỗi buổi 75 phút', weightHeightBeforeLifting: '65 kg, 168 cm',
  trainingGoal: 'Giảm mỡ, giữ cơ', medicalHistory: 'Không', foodBudgetReadiness: 'Sẵn sàng', activityLevel: 'Vừa',
  stressLevel: 'Vừa', sleepDuration: '7–8 tiếng', currentDiet: 'Ăn 3 bữa chính', mealAndTrainingTimes: 'Ăn 7h, 12h, 19h. Tập 17h30–19h.',
  sessionsPerWeek: '4 buổi', programStyle: 'Upper / Lower', bodyPhotoUrl: 'https://drive.google.com/open?id=abc',
  expectations: 'Giáo án rõ ràng', previousCoaching: 'Chưa từng', investmentReason: 'Muốn khỏe hơn',
};

/**
 * DB của compose giữ dữ liệu giữa các lần chạy → mỗi email viết trong feature được gắn một hậu tố +e2e… cố định trong scenario.
 * Giữ nguyên hoa/thường và khoảng trắng người dùng gõ ("QUAN@gmail.com " → "QUAN+e2e…@gmail.com ") để server tự chuẩn hoá.
 */
function realEmail(world: E2EWorld, written: string): string {
  const key = written.trim().toLowerCase();
  world.emailSuffix[key] ??= `+e2e${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return written.replace('@', `${world.emailSuffix[key]}@`);
}

async function adminToken(world: E2EWorld): Promise<string> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  return ((await res.json()) as { accessToken: string }).accessToken;
}

async function createStudent(world: E2EWorld, writtenEmail: string, complete: boolean): Promise<void> {
  const token = await adminToken(world);
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/students`, {
    data: { fullName: FULL_NAME, email: realEmail(world, writtenEmail), ...(complete ? { registration: FULL_REGISTRATION } : {}) },
    headers: { authorization: `Bearer ${token}` },
  });
  expect(res.status()).toBe(201);
  const body = (await res.json()) as { id: string; email: string };
  world.student = { id: body.id, fullName: FULL_NAME, email: body.email };
}

/** Đợi tới khi hộp thư Capture có `count` thư cùng tiêu đề gửi tới `to`; trả thư mới nhất. */
async function waitForMail(world: E2EWorld, to: string, subject: string, count = 1): Promise<CapturedMail> {
  const deadline = Date.now() + 15_000;
  for (;;) {
    const res = await world.page.request.get(`${ADMIN_URL}/api/test/emails`, { params: { to: to.trim().toLowerCase() } });
    expect(res.ok()).toBe(true);
    const mails = ((await res.json()) as CapturedMail[]).filter((m) => m.subject === subject);
    if (mails.length >= count) return mails.sort((a, b) => Date.parse(b.sentAt) - Date.parse(a.sentAt))[0];
    if (Date.now() > deadline) throw new Error(`Không thấy ${count} thư "${subject}" gửi tới ${to} (có ${mails.length})`);
    await new Promise((r) => setTimeout(r, 250));
  }
}

function tempPasswordOf(mail: CapturedMail): string {
  const match = /Mật khẩu tạm:\s*(\S+)/.exec(mail.textBody);
  if (!match) throw new Error(`Thư thông tin đăng nhập không có dòng "Mật khẩu tạm:"\n${mail.textBody}`);
  return match[1];
}

async function sendCredentialsByApi(world: E2EWorld): Promise<void> {
  const token = await adminToken(world);
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/students/${world.student!.id}/credentials`,
    { data: {}, headers: { authorization: `Bearer ${token}` } });
  expect(res.status()).toBe(200);
  world.tempPassword = tempPasswordOf(await waitForMail(world, world.student!.email, CREDENTIALS_SUBJECT));
}

async function activateByApi(world: E2EWorld, password: string): Promise<void> {
  const login = await world.page.request.post(`${CLIENT_URL}/api/auth/login`,
    { data: { email: world.student!.email, password: world.tempPassword, app: 'client' } });
  expect(login.status()).toBe(200);
  const { activationToken } = (await login.json()) as { activationToken: string };
  const res = await world.page.request.post(`${CLIENT_URL}/api/auth/activate`,
    { data: { activationToken, password, confirmPassword: password } });
  expect(res.status()).toBe(200);
}

/** Ngày hôm nay theo giờ Việt Nam (UTC+7), dạng dd/MM/yyyy. */
function vnToday(): string {
  const d = new Date(Date.now() + 7 * 60 * 60 * 1000);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(d.getUTCDate())}/${pad(d.getUTCMonth() + 1)}/${d.getUTCFullYear()}`;
}

// ---- features/hoc-vien/tai-khoan/thong-tin-dang-nhap.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-002
Given('một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
});

When('coach gửi thông tin đăng nhập cho học viên đó', async function (this: E2EWorld) {
  await this.page.goto(`${ADMIN_URL}/hoc-vien/${this.student!.id}`);
  await this.page.getByRole('button', { name: 'Gửi thông tin đăng nhập', exact: true }).click();
  await expect(this.page.getByRole('button', { name: 'Gửi lại thông tin đăng nhập', exact: true })).toBeVisible();
});

Then('học viên đó nhận được email thông tin đăng nhập', async function (this: E2EWorld) {
  const mail = await waitForMail(this, this.student!.email, CREDENTIALS_SUBJECT);
  expect(mail.textBody).toContain(`${CLIENT_URL}/login`);
  expect(mail.textBody).toContain(this.student!.email);
  this.tempPassword = tempPasswordOf(mail);
});

Then('coach thấy tài khoản đã được gửi thông tin đăng nhập', async function (this: E2EWorld) {
  const card = this.page.getByRole('region', { name: 'Tài khoản đăng nhập' });
  await expect(card).toContainText(/Đã gửi lúc \d{2}\/\d{2} \d{2}:\d{2}, mật khẩu tạm còn hạn đến \d{2}\/\d{2} \d{2}:\d{2}/);
  await expect(card).toContainText('Gửi thông tin đăng nhập lần đầu.');
  await expect(this.page.locator('body')).not.toContainText(this.tempPassword); // @BR-f01-p1-tai-khoan-hoc-vien-025
});

// @BR-f01-p1-tai-khoan-hoc-vien-007
Given('một hồ sơ học viên với email {string}, tài khoản Chờ kích hoạt', async function (this: E2EWorld, email: string) {
  await createStudent(this, email, false);
});

Given('chưa có tài khoản nào dùng email {string}', function (this: E2EWorld, email: string) {
  realEmail(this, email); // hậu tố mới trong scenario này → chắc chắn chưa ai dùng
});

When('coach sửa email của học viên đó thành {string}', async function (this: E2EWorld, email: string) {
  const id = this.student!.id;
  await this.page.goto(`${ADMIN_URL}/hoc-vien/${id}/sua`);
  const field = this.page.getByLabel('Email đăng nhập');
  await expect(field).toHaveValue(this.student!.email);
  await field.fill(realEmail(this, email));
  await this.page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
  await expect(this.page).toHaveURL(new RegExp(`/hoc-vien/${id}$`));
});

Then('hồ sơ đó có email {string}', async function (this: E2EWorld, email: string) {
  await expect(this.page.locator('.meta')).toContainText(realEmail(this, email).trim().toLowerCase());
});

// ---- features/hoc-vien/tai-khoan/kich-hoat.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-011
Given('một học viên vừa được gửi mật khẩu tạm', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
  await sendCredentialsByApi(this);
});

When('học viên đăng nhập web học viên bằng mật khẩu tạm đó', async function (this: E2EWorld) {
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.login(this.page, this.student!.email, this.tempPassword);
});

Then('học viên được đưa tới bước đặt mật khẩu riêng', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(/\/kich-hoat$/);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt mật khẩu riêng' })).toBeVisible();
  expect(await this.page.evaluate(() => localStorage.getItem('dbx.client.rt'))).toBeNull(); // chưa có phiên
});

// @BR-f01-p1-tai-khoan-hoc-vien-016
// Compose chạy đồng hồ thật nên không đặt được "hôm nay" của server; ngày cụ thể trong feature được kiểm ở @api
// (Reqnroll, đồng hồ giả). Ở @web, "ngày kích hoạt" phải là hôm nay theo giờ Việt Nam.
Given('hôm nay là {string}', function (this: E2EWorld, _date: string) { /* xem chú thích trên */ });

Given('học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
  await sendCredentialsByApi(this);
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.login(this.page, this.student!.email, this.tempPassword);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt mật khẩu riêng' })).toBeVisible();
});

When('học viên lưu mật khẩu riêng {string}', async function (this: E2EWorld, password: string) {
  await this.page.getByLabel('Mật khẩu mới', { exact: true }).fill(password);
  await this.page.getByLabel('Nhập lại mật khẩu', { exact: true }).fill(password);
  await this.page.getByRole('button', { name: 'Lưu mật khẩu và vào trang Hôm nay' }).click();
});

Then('coach thấy tài khoản của học viên đó Đã kích hoạt ngày {string}', async function (this: E2EWorld, _date: string) {
  const today = vnToday();
  const admin = await this.openPage(`${ADMIN_URL}/login`);
  await this.login(admin, ADMIN.email, ADMIN.password);
  await expect(admin).not.toHaveURL(/\/login/);
  await admin.goto(`${ADMIN_URL}/hoc-vien/${this.student!.id}`);
  await expect(admin.getByRole('region', { name: 'Tài khoản đăng nhập' })).toContainText(`Đã kích hoạt ngày ${today}`);
  await expect(admin.locator('.meta')).toContainText(`Kích hoạt ${today}`);
});

// ---- features/hoc-vien/tai-khoan/dat-lai-mat-khau.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-022
Given('học viên có tài khoản Đã kích hoạt với email {string}', async function (this: E2EWorld, email: string) {
  await createStudent(this, email, true);
  await sendCredentialsByApi(this);
  await activateByApi(this, OWN_PASSWORD);
});

When('một người xin đặt lại mật khẩu cho email {string}', async function (this: E2EWorld, email: string) {
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.page.getByRole('button', { name: 'Quên mật khẩu?' }).click();
  await expect(this.page).toHaveURL(/\/quen-mat-khau$/);
  await this.page.getByLabel('Email').fill(realEmail(this, email));
  await this.page.getByRole('button', { name: 'Gửi link đặt lại' }).click();
  await expect(this.page.getByRole('status')).toHaveText(FORGOT_MESSAGE);
});

Then('email {string} nhận được email có link đặt lại mật khẩu', async function (this: E2EWorld, email: string) {
  const mail = await waitForMail(this, realEmail(this, email), RESET_SUBJECT);
  const link = /(https?:\/\/\S+\/dat-lai-mat-khau#t=[A-Za-z0-9_-]+)/.exec(mail.textBody)?.[1];
  expect(link, 'thư đặt lại phải có link /dat-lai-mat-khau#t=…').toBeTruthy();
  await this.page.goto(link!);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt lại mật khẩu' })).toBeVisible();
  await expect(this.page.getByRole('button', { name: 'Lưu mật khẩu mới' })).toBeVisible();
});
