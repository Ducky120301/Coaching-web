import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StatusBadgeComponent } from './status-badge.component';

@Component({
  imports: [StatusBadgeComponent],
  template: `<dbx-status-badge tone="warn">Chờ kích hoạt</dbx-status-badge><dbx-status-badge tone="ok" [dense]="true">Đang coaching</dbx-status-badge>`,
})
class HostComponent {}

describe('StatusBadgeComponent', () => {
  it('renders the label with its tone and density', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const [warn, ok] = fixture.nativeElement.querySelectorAll('.badge') as NodeListOf<HTMLElement>;
    expect(warn.textContent?.trim()).toBe('Chờ kích hoạt');
    expect(warn.classList).toContain('warn');
    expect(warn.classList).not.toContain('dense');
    expect(ok.classList).toContain('ok');
    expect(ok.classList).toContain('dense');
  });
});
