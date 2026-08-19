import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { BuchungenService } from '../shared/api/buchungen.service';
import { BuchungDetail } from '../shared/models/buchung-detail';
import { Buchungsdetail } from './buchungsdetail';

// [ASSUMPTION-Korrektur: jsdom 28 reflektiert zwar die `open`-IDL-Eigenschaft von
// HTMLDialogElement auf das Attribut, implementiert aber showModal()/close() nicht
// (leere Implementierungsklasse in jsdom) — Polyfill hier, da echte Browser dies
// nativ unterstützen und nur die Testumgebung die Lücke hat.]
if (!HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function (this: HTMLDialogElement) {
    this.open = true;
  };
}
if (!HTMLDialogElement.prototype.close) {
  HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
    this.open = false;
  };
}

describe('Buchungsdetail', () => {
  const buchung: BuchungDetail = {
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
    positionen: [
      {
        sitzplatzCode: 'A1',
        preiskategorieId: 'E1-A',
        preiskategorieName: 'Kategorie A',
        preisSnapshot: 32,
      },
    ],
  };

  function createFixture(
    serviceStub: Pick<BuchungenService, 'buchungAbrufen'> &
      Partial<Pick<BuchungenService, 'buchungStornieren'>>,
  ) {
    TestBed.configureTestingModule({
      imports: [Buchungsdetail],
      providers: [
        provideRouter([]),
        { provide: BuchungenService, useValue: serviceStub },
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ referenz: 'ABCD1234' })) },
        },
      ],
    });
    return TestBed.createComponent(Buchungsdetail);
  }

  it('zeigt Veranstaltungstitel, Zeitpunkt, Spielstätte/Raum, Positionen und Gesamtpreis', () => {
    const fixture = createFixture({ buchungAbrufen: () => of(buchung) });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Kammerkonzert Frühling');
    expect(text).toContain('19:30');
    expect(text).toContain('Stadthalle Nordpark');
    expect(text).toContain('Kleiner Saal');
    expect(text).toContain('A1');
    expect(text).toContain('Kategorie A');
    expect(text).toContain('Gesamtpreis');
  });

  it('zeigt einen Ladehinweis, solange die Buchung noch lädt', () => {
    const nichtAufgeloest = new Subject<BuchungDetail>();
    const fixture = createFixture({ buchungAbrufen: () => nichtAufgeloest.asObservable() });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Buchung wird geladen');
    expect(fixture.nativeElement.querySelector('.buchungsdetail')).toBeNull();
  });

  it('zeigt eine Fehlermeldung, wenn der Abruf fehlschlägt', () => {
    const fixture = createFixture({
      buchungAbrufen: () => throwError(() => new Error('nicht gefunden')),
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Keine Buchung mit dieser Referenz gefunden.',
    );
  });

  it('rendert app-badge-status mit status="Storniert", wenn die Buchung storniert ist', () => {
    const fixture = createFixture({
      buchungAbrufen: () => of({ ...buchung, status: 'Storniert' as const }),
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('storniert');
    expect(fixture.nativeElement.querySelector('.badge-status--storniert')).not.toBeNull();
  });

  it('zeigt den "Stornieren"-Button bei aktiver Buchung', () => {
    const fixture = createFixture({ buchungAbrufen: () => of(buchung) });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).not.toBeNull();
  });

  it('verbirgt den "Stornieren"-Button bei bereits stornierter Buchung', () => {
    const fixture = createFixture({
      buchungAbrufen: () => of({ ...buchung, status: 'Storniert' as const }),
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).toBeNull();
  });

  it('öffnet den Bestätigungsdialog bei Klick auf "Stornieren"', () => {
    const fixture = createFixture({ buchungAbrufen: () => of(buchung) });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.open).toBe(true);
  });

  it('schließt den Dialog bei Klick auf "Abbrechen", ohne buchungStornieren aufzurufen', () => {
    const stornierenSpy = vi.fn();
    const fixture = createFixture({
      buchungAbrufen: () => of(buchung),
      buchungStornieren: stornierenSpy,
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__abbrechen').click();
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.open).toBe(false);
    expect(stornierenSpy).not.toHaveBeenCalled();
  });

  it('zeigt bei erfolgreicher Bestätigung banner-success, aktualisierten Status und verbirgt den Button', () => {
    const fixture = createFixture({
      buchungAbrufen: () => of(buchung),
      buchungStornieren: () => of({ ...buchung, status: 'Storniert' as const }),
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__bestaetigen').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Buchung storniert. Die Plätze sind wieder frei.',
    );
    expect(fixture.nativeElement.textContent).toContain('storniert');
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).toBeNull();
    expect(fixture.nativeElement.querySelector('dialog')).toBeNull();
  });

  it('zeigt bei fehlgeschlagener Bestätigung (409, bereits storniert) banner-error mit dem exakten Text und verbirgt den Button', () => {
    const fixture = createFixture({
      buchungAbrufen: () => of(buchung),
      buchungStornieren: () =>
        throwError(() => new HttpErrorResponse({ status: 409, statusText: 'Conflict' })),
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__bestaetigen').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Diese Buchung wurde bereits storniert.');
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).toBeNull();
  });

  it('zeigt bei fehlgeschlagener Bestätigung (Netzwerk-/Serverfehler ungleich 409) eine generische Fehlermeldung statt "bereits storniert" und behält den Button', () => {
    const fixture = createFixture({
      buchungAbrufen: () => of(buchung),
      buchungStornieren: () =>
        throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' })),
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__bestaetigen').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Stornierung fehlgeschlagen. Bitte versuchen Sie es erneut.',
    );
    expect(fixture.nativeElement.textContent).not.toContain('bereits storniert');
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).not.toBeNull();
  });

  it('deaktiviert den "Abbrechen"-Button, während eine Stornierung läuft', () => {
    const nichtAufgeloest = new Subject<BuchungDetail>();
    const fixture = createFixture({
      buchungAbrufen: () => of(buchung),
      buchungStornieren: () => nichtAufgeloest.asObservable(),
    });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__bestaetigen').click();
    fixture.detectChanges();

    const abbrechenButton = fixture.nativeElement.querySelector(
      '.stornieren-dialog__abbrechen',
    ) as HTMLButtonElement;
    expect(abbrechenButton.disabled).toBe(true);
  });

  it('verbirgt den alten banner-success wieder, sobald über die Route eine andere Referenz geladen wird', () => {
    const andereBuchung: BuchungDetail = { ...buchung, referenz: 'ZZZZ9999' };
    const paramMap$ = new Subject<ParamMap>();
    TestBed.configureTestingModule({
      imports: [Buchungsdetail],
      providers: [
        provideRouter([]),
        {
          provide: BuchungenService,
          useValue: {
            buchungAbrufen: (referenz: string) =>
              of(referenz === buchung.referenz ? buchung : andereBuchung),
            buchungStornieren: () => of({ ...buchung, status: 'Storniert' as const }),
          },
        },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchungsdetail);
    paramMap$.next(convertToParamMap({ referenz: buchung.referenz }));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.buchungsdetail__stornieren').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.stornieren-dialog__bestaetigen').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Buchung storniert.');

    paramMap$.next(convertToParamMap({ referenz: andereBuchung.referenz }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Buchung storniert.');
    expect(fixture.nativeElement.querySelector('.buchungsdetail__stornieren')).not.toBeNull();
  });
});
