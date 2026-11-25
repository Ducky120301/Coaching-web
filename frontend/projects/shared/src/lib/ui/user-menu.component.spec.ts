import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthService } from '../auth/auth.service';
import { UserMenuComponent } from './user-menu.component';

@Component({
  imports: [UserMenuComponent],
  template: `<dbx-user-menu [name]="name" roleLabel="Coach" /><p id="outside">x</p>`,
})
class HostComponent { name = 'Coach Duobexiuuu'; }

describe('UserMenuComponent', () => {
  const logout = vi.fn();
  let fixture: ComponentFixture<HostComponent>;
  const el = (): HTMLElement => fixture.nativeElement;
  const trigger = (): HTMLButtonElement => el().querySelector('.user-block')!;
  const menu = (): HTMLElement | null => el().querySelector('.menu');

  beforeEach(async () => {
    logout.mockReset();
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: AuthService, useValue: { logout } }],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    document.body.appendChild(el());
    fixture.detectChanges();
  });

  afterEach(() => el().remove());

  it('toggles open on trigger click', async () => {
    trigger().click();
    fixture.detectChanges();
    expect(menu()).not.toBeNull();
    trigger().click();
    fixture.detectChanges();
    expect(menu()).toBeNull();
  });

  it('closes on Escape and focuses the trigger', () => {
    trigger().click();
    fixture.detectChanges();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(menu()).toBeNull();
    expect(document.activeElement).toBe(trigger());
  });

  it('closes on outside click, stays open on inside click', () => {
    trigger().click();
    fixture.detectChanges();
    menu()!.click();
    fixture.detectChanges();
    expect(menu()).not.toBeNull();
    el().querySelector<HTMLElement>('#outside')!.click();
    fixture.detectChanges();
    expect(menu()).toBeNull();
  });

  it('computes initials with fallback', () => {
    expect(el().querySelector('.avatar')!.textContent).toBe('CD');
    fixture.componentInstance.name = '';
    fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.name = '';
    fixture.detectChanges();
    expect(el().querySelector('.avatar')!.textContent).toBe('?');
  });

  it('logout calls AuthService.logout', () => {
    trigger().click();
    fixture.detectChanges();
    menu()!.querySelector('button')!.click();
    expect(logout).toHaveBeenCalled();
  });
});
