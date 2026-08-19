import { TestBed } from '@angular/core/testing';
import { BadgeStatus } from './badge-status';

describe('BadgeStatus', () => {
  function createFixture(status: 'Aktiv' | 'Storniert') {
    const fixture = TestBed.createComponent(BadgeStatus);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    return fixture;
  }

  it('zeigt den Text "aktiv" und die Modifier-Klasse bei Status Aktiv', () => {
    const fixture = createFixture('Aktiv');
    const badge: HTMLElement = fixture.nativeElement.querySelector('.badge-status');

    expect(badge.textContent).toContain('aktiv');
    expect(badge.classList).toContain('badge-status--aktiv');
    expect(badge.classList).not.toContain('badge-status--storniert');
  });

  it('zeigt den Text "storniert" und die Modifier-Klasse bei Status Storniert', () => {
    const fixture = createFixture('Storniert');
    const badge: HTMLElement = fixture.nativeElement.querySelector('.badge-status');

    expect(badge.textContent).toContain('storniert');
    expect(badge.classList).toContain('badge-status--storniert');
    expect(badge.classList).not.toContain('badge-status--aktiv');
  });
});
