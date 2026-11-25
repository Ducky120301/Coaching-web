import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { ClientHomeComponent } from './client-home.component';

describe('ClientHomeComponent', () => {
  it('chào bằng tên cuối của họ tên', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: { user: signal({ fullName: 'Phạm Hoàng Quân' }) } }],
    });
    const fixture = TestBed.createComponent(ClientHomeComponent);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('h1').textContent).toContain('Chào Quân');
  });
});
