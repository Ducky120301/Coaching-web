import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { AdminShellComponent } from './admin-shell.component';

describe('AdminShellComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
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
});
