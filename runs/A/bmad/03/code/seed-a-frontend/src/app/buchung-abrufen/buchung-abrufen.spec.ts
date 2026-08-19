import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { BuchungenService } from '../shared/api/buchungen.service';
import { BuchungDetail } from '../shared/models/buchung-detail';
import { BuchungAbrufen } from './buchung-abrufen';

describe('BuchungAbrufen', () => {
  const buchungDetail: BuchungDetail = {
    referenz: 'ABCD1234',
    veranstaltungId: 'E1',
    veranstaltungTitel: 'Kammerkonzert Frühling',
    veranstaltungZeitpunkt: '2026-09-05T19:30:00+02:00',
    spielstaetteName: 'Stadthalle Nordpark',
    raumName: 'Kleiner Saal',
    name: 'Max Mustermann',
    email: 'max@example.com',
    status: 'Aktiv',
    gesamtpreis: 32,
    positionen: [],
  };

  function createFixture(serviceStub: Pick<BuchungenService, 'buchungAbrufen'>) {
    TestBed.configureTestingModule({
      imports: [BuchungAbrufen],
      providers: [provideRouter([]), { provide: BuchungenService, useValue: serviceStub }],
    });
    return TestBed.createComponent(BuchungAbrufen);
  }

  it('deaktiviert den Absenden-Button bei leerem Eingabefeld', () => {
    const fixture = createFixture({ buchungAbrufen: () => of(buchungDetail) });
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.buchung-abrufen__absenden',
    );
    expect(button.disabled).toBe(true);
  });

  it('zeigt bei Fehlschlag den Banner mit exaktem Fehlertext', () => {
    const fixture = createFixture({
      buchungAbrufen: () => throwError(() => new Error('nicht gefunden')),
    });
    fixture.detectChanges();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('.buchung-abrufen__input');
    input.value = 'ZZZZZZZZ';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement
      .querySelector('.buchung-abrufen__formular')
      .dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const banner: HTMLElement = fixture.nativeElement.querySelector('.banner-error');
    expect(banner.textContent).toContain('Keine Buchung mit dieser Referenz gefunden.');
  });

  it('navigiert bei Erfolg zur Buchungsdetailseite', () => {
    const fixture = createFixture({ buchungAbrufen: () => of(buchungDetail) });
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    fixture.detectChanges();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('.buchung-abrufen__input');
    input.value = 'ABCD1234';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement
      .querySelector('.buchung-abrufen__formular')
      .dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', 'ABCD1234']);
  });
});
