import { TestBed } from '@angular/core/testing';

import { ErrorBanner } from './error-banner';

describe('ErrorBanner', () => {
  it('renders the message text with role="alert" and aria-live="assertive"', () => {
    TestBed.configureTestingModule({ imports: [ErrorBanner] });
    const fixture = TestBed.createComponent(ErrorBanner);
    fixture.componentRef.setInput('message', 'Platz B3 ist inzwischen vergeben.');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const root = compiled.querySelector('.error-banner') as HTMLElement;

    expect(root.textContent?.trim()).toBe('Platz B3 ist inzwischen vergeben.');
    expect(root.getAttribute('role')).toBe('alert');
    expect(root.getAttribute('aria-live')).toBe('assertive');
  });
});
