import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { Buchungsreferenz } from './buchungsreferenz';

describe('Buchungsreferenz', () => {
  function createFixture(referenz = 'ABCD1234') {
    const fixture = TestBed.createComponent(Buchungsreferenz);
    fixture.componentRef.setInput('referenz', referenz);
    fixture.detectChanges();
    return fixture;
  }

  it('zeigt die Referenz in mono-Schrift an', () => {
    const fixture = createFixture('ABCD1234');
    expect(fixture.nativeElement.querySelector('.buchungsreferenz__code').textContent).toContain('ABCD1234');
  });

  it('kopiert die Referenz über die Clipboard-API und zeigt kurzzeitig eine Bestätigung', async () => {
    const urspruenglichesClipboard = navigator.clipboard;
    vi.useFakeTimers();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });

    const fixture = createFixture('ABCD1234');
    fixture.nativeElement.querySelector('.buchungsreferenz__kopieren').click();
    await Promise.resolve();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith('ABCD1234');
    expect(fixture.nativeElement.querySelector('.buchungsreferenz__kopieren').textContent).toContain('Kopiert');

    vi.advanceTimersByTime(2000);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.buchungsreferenz__kopieren').textContent).toContain('Kopieren');

    vi.useRealTimers();
    Object.defineProperty(navigator, 'clipboard', { value: urspruenglichesClipboard, configurable: true });
  });
});
