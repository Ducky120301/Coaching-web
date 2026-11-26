import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, Routes, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { AdminShellComponent } from './admin-shell.component';

@Component({ template: '' })
class BlankPage {}

describe('AdminShellComponent', () => {
  function setup(routes: Routes = []) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(AdminShellComponent);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement };
  }

  it('shows the coach name from /api/admin/profile in the rail user menu', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/profile').flush({ id: '1', displayName: 'Coach Duobexiuuu', email: 'c@x.vn' });
    fixture.detectChanges();

    expect(el.querySelector('aside.rail dbx-user-menu')?.textContent).toContain('Coach Duobexiuuu');
    expect(el.querySelector('nav a')?.textContent?.trim()).toBe('Dashboard');
  });

  it('keeps the rail usable when the profile cannot be loaded', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/profile').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(el.querySelector('aside.rail dbx-user-menu')).not.toBeNull();
  });

  it('lists the library in the rail after the student items', () => {
    const { el } = setup();

    const links = [...el.querySelectorAll('nav[aria-label="Admin"] a')].map((a) => [a.textContent?.trim(), a.getAttribute('href')]);
    expect(links).toEqual([
      ['Dashboard', '/dashboard'],
      ['Tạo hồ sơ học viên', '/hoc-vien/moi'],
      ['Thư viện bài tập', '/thu-vien/bai-tap'],
    ]);
  });

  it('keeps the library rail item current when navigating inside the app from the list to the add form', async () => {
    const { fixture, el } = setup([
      { path: 'thu-vien/bai-tap', component: BlankPage },
      { path: 'thu-vien/bai-tap/moi', component: BlankPage },
    ]);
    const router = TestBed.inject(Router);
    const current = () => [...el.querySelectorAll('nav[aria-label="Admin"] a[aria-current="page"]')].map((a) => a.textContent?.trim());

    await router.navigateByUrl('/thu-vien/bai-tap');
    await fixture.whenStable();
    expect(current()).toEqual(['Thư viện bài tập']);

    await router.navigateByUrl('/thu-vien/bai-tap/moi');
    await fixture.whenStable();
    expect(current()).toEqual(['Thư viện bài tập']);
  });
});
