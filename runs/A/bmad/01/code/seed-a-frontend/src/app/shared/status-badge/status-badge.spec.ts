import { TestBed } from '@angular/core/testing';

import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  it('renders "aktiv" without the cancelled class when status is Active', () => {
    TestBed.configureTestingModule({ imports: [StatusBadge] });
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'Active');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const root = compiled.querySelector('.status-badge') as HTMLElement;

    expect(root.textContent?.trim()).toBe('aktiv');
    expect(root.classList.contains('status-badge--cancelled')).toBe(false);
  });

  it('renders "storniert" with the cancelled class when status is Cancelled', () => {
    TestBed.configureTestingModule({ imports: [StatusBadge] });
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'Cancelled');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const root = compiled.querySelector('.status-badge') as HTMLElement;

    expect(root.textContent?.trim()).toBe('storniert');
    expect(root.classList.contains('status-badge--cancelled')).toBe(true);
  });
});
