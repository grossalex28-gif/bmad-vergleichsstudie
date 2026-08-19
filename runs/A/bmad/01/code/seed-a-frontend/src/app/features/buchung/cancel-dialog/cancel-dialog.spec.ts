import { TestBed } from '@angular/core/testing';

import { CancelDialog } from './cancel-dialog';

describe('CancelDialog', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancelDialog]
    }).compileComponents();
  });

  it('emits confirm on click of "Stornieren"', () => {
    const fixture = TestBed.createComponent(CancelDialog);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.confirm.subscribe(() => (emitted = true));

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement).click();

    expect(emitted).toBe(true);
  });

  it('emits cancel on click of "Abbrechen"', () => {
    const fixture = TestBed.createComponent(CancelDialog);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.cancel.subscribe(() => (emitted = true));

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.cancel-dialog__cancel') as HTMLButtonElement).click();

    expect(emitted).toBe(true);
  });

  it('emits cancel when Escape is pressed', () => {
    const fixture = TestBed.createComponent(CancelDialog);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.cancel.subscribe(() => (emitted = true));

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(emitted).toBe(true);
  });

  it('does not emit cancel when Escape is pressed while cancelling is true', () => {
    const fixture = TestBed.createComponent(CancelDialog);
    fixture.componentRef.setInput('cancelling', true);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.cancel.subscribe(() => (emitted = true));

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(emitted).toBe(false);
  });

  it('disables both buttons and shows "Wird storniert …" when cancelling is true', () => {
    const fixture = TestBed.createComponent(CancelDialog);
    fixture.componentRef.setInput('cancelling', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cancelButton = compiled.querySelector('.cancel-dialog__cancel') as HTMLButtonElement;
    const confirmButton = compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement;

    expect(cancelButton.disabled).toBe(true);
    expect(confirmButton.disabled).toBe(true);
    expect(confirmButton.textContent?.trim()).toBe('Wird storniert …');
  });
});
