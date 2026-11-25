import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { AdminHomeComponent } from './admin-home.component';

describe('AdminHomeComponent', () => {
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
    const fixture = TestBed.createComponent(AdminHomeComponent);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController) };
  }

  it('renders greeting on success', async () => {
    const { fixture, http } = setup();
    http.expectOne('/api/admin/profile').flush({ id: '1', displayName: 'Coach Duobexiuuu', email: 'c@x.vn' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1').textContent).toContain('Xin chào, Coach Duobexiuuu');
    expect(fixture.nativeElement.textContent).toContain('c@x.vn');
  });

  it('renders error line on 500', () => {
    const { fixture, http } = setup();
    http.expectOne('/api/admin/profile').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Không tải được thông tin coach. Vui lòng tải lại trang.');
  });
});
