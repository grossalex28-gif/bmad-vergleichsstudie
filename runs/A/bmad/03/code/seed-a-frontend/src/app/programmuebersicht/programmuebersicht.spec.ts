import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { Veranstaltung } from '../shared/models/veranstaltung';
import { Programmuebersicht } from './programmuebersicht';

describe('Programmuebersicht', () => {
  const veranstaltungen: Veranstaltung[] = [
    {
      id: 'E1',
      titel: 'Kammerkonzert Frühling',
      spielstaetteId: 'V1',
      spielstaetteName: 'Stadthalle Nordpark',
      zeitpunkt: '2026-09-05T19:30:00+02:00'
    },
    {
      id: 'E2',
      titel: 'Comedy-Abend',
      spielstaetteId: 'V1',
      spielstaetteName: 'Stadthalle Nordpark',
      zeitpunkt: '2026-09-20T21:00:00+02:00'
    }
  ];

  function createFixture(serviceStub: Pick<VeranstaltungenService, 'getVeranstaltungen'>) {
    TestBed.configureTestingModule({
      imports: [Programmuebersicht],
      providers: [provideRouter([]), { provide: VeranstaltungenService, useValue: serviceStub }]
    });
    return TestBed.createComponent(Programmuebersicht);
  }

  function setzeDatumsfeld(fixture: ReturnType<typeof createFixture>, id: string, wert: string) {
    const input: HTMLInputElement = fixture.nativeElement.querySelector(`#${id}`);
    input.value = wert;
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  it('rendert für jede geladene Veranstaltung eine event-card mit Titel, Spielstätte, Datum und Uhrzeit', () => {
    const fixture = createFixture({ getVeranstaltungen: () => of(veranstaltungen) });
    fixture.detectChanges();

    const karten = fixture.nativeElement.querySelectorAll('app-event-card');
    expect(karten.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Kammerkonzert Frühling');
    expect(fixture.nativeElement.textContent).toContain('Stadthalle Nordpark');
    expect(fixture.nativeElement.textContent).toContain('19:30');
  });

  it('zeigt Skeleton-Einträge während des Ladens an, keine event-card', () => {
    const nichtAufgeloest = new Subject<Veranstaltung[]>();
    const fixture = createFixture({ getVeranstaltungen: () => nichtAufgeloest.asObservable() });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-event-card').length).toBe(0);
    expect(fixture.nativeElement.querySelectorAll('.event-card-skeleton').length).toBeGreaterThan(0);
  });

  it('zeigt bei leerer Liste den Leerzustand-Text statt Karten oder leerer Fläche', () => {
    const fixture = createFixture({ getVeranstaltungen: () => of([]) });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-event-card').length).toBe(0);
    expect(fixture.nativeElement.querySelectorAll('.event-card-skeleton').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Aktuell sind keine Veranstaltungen verfügbar.');
  });

  it('zeigt bei fehlgeschlagenem Laden eine Fehlermeldung statt dauerhaftem Skeleton', () => {
    const fixture = createFixture({ getVeranstaltungen: () => throwError(() => new Error('Netzwerkfehler')) });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-event-card').length).toBe(0);
    expect(fixture.nativeElement.querySelectorAll('.event-card-skeleton').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Veranstaltungen konnten nicht geladen werden.');
  });

  it('protokolliert einen Fehler, wenn das Laden der Spielstätten-Dropdown-Optionen fehlschlägt, ohne die Hauptliste zu beeinträchtigen', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    const getVeranstaltungen = vi.fn().mockImplementation((...args: unknown[]) =>
      args.length === 0 ? throwError(() => new Error('Netzwerkfehler')) : of(veranstaltungen)
    );
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    expect(consoleError).toHaveBeenCalledWith(
      'Spielstätten für Filter-Dropdown konnten nicht geladen werden',
      expect.any(Error)
    );
    expect(fixture.nativeElement.querySelectorAll('app-event-card').length).toBe(2);

    consoleError.mockRestore();
  });

  it('löst bei Filteränderung einen neuen getVeranstaltungen-Aufruf mit den erwarteten von/bis-Werten aus', () => {
    const getVeranstaltungen = vi.fn().mockReturnValue(of(veranstaltungen));
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    expect(getVeranstaltungen).toHaveBeenCalledWith(null, null, null);

    setzeDatumsfeld(fixture, 'filter-bar-von', '2026-09-01');

    expect(getVeranstaltungen).toHaveBeenCalledWith('2026-09-01', null, null);

    setzeDatumsfeld(fixture, 'filter-bar-bis', '2026-09-30');

    expect(getVeranstaltungen).toHaveBeenCalledWith('2026-09-01', '2026-09-30', null);
  });

  it('lädt die Spielstätten-Dropdown-Optionen einmalig ohne Argumente, getrennt vom reaktiven Filter-Aufruf', () => {
    const getVeranstaltungen = vi.fn().mockReturnValue(of(veranstaltungen));
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    expect(getVeranstaltungen).toHaveBeenCalledWith();
    expect(getVeranstaltungen).toHaveBeenCalledWith(null, null, null);
  });

  it('zeigt im Spielstätten-Dropdown die aus der Antwort extrahierten, eindeutigen Spielstätten', () => {
    const zweiSpielstaetten: Veranstaltung[] = [
      {
        id: 'E1',
        titel: 'Kammerkonzert Frühling',
        spielstaetteId: 'V1',
        spielstaetteName: 'Stadthalle Nordpark',
        zeitpunkt: '2026-09-05T19:30:00+02:00'
      },
      {
        id: 'E2',
        titel: 'Comedy-Abend',
        spielstaetteId: 'V2',
        spielstaetteName: 'Kulturhaus Südtor',
        zeitpunkt: '2026-09-20T21:00:00+02:00'
      }
    ];
    const fixture = createFixture({ getVeranstaltungen: () => of(zweiSpielstaetten) });
    fixture.detectChanges();

    const optionen: HTMLOptionElement[] = fixture.nativeElement.querySelectorAll(
      '#filter-bar-spielstaette option'
    );
    const optionsTexte = Array.from(optionen).map((option) => option.textContent?.trim());
    expect(optionsTexte).toEqual(['Alle', 'Kulturhaus Südtor', 'Stadthalle Nordpark']);
  });

  it('löst bei Auswahl im Spielstätten-Dropdown einen neuen getVeranstaltungen-Aufruf mit der gewählten spielstaetteId aus', () => {
    const getVeranstaltungen = vi.fn().mockReturnValue(of(veranstaltungen));
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#filter-bar-spielstaette');
    select.value = 'V1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getVeranstaltungen).toHaveBeenCalledWith(null, null, 'V1');
  });

  it('löst bei gleichzeitig gesetztem Datumsbereich und Spielstätte einen Aufruf mit allen drei Werten aus', () => {
    const getVeranstaltungen = vi.fn().mockReturnValue(of(veranstaltungen));
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    setzeDatumsfeld(fixture, 'filter-bar-von', '2026-09-01');
    setzeDatumsfeld(fixture, 'filter-bar-bis', '2026-09-30');

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#filter-bar-spielstaette');
    select.value = 'V1';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getVeranstaltungen).toHaveBeenCalledWith('2026-09-01', '2026-09-30', 'V1');
  });

  it('zeigt bei gefilterter leerer Trefferliste den gefilterten Leerzustand-Text statt des generellen', () => {
    const fixture = createFixture({ getVeranstaltungen: () => of([]) });
    fixture.detectChanges();

    setzeDatumsfeld(fixture, 'filter-bar-von', '2026-09-06');
    setzeDatumsfeld(fixture, 'filter-bar-bis', '2026-09-11');

    expect(fixture.nativeElement.textContent).toContain(
      'Keine Veranstaltungen im gewählten Zeitraum/an dieser Spielstätte.'
    );
    expect(fixture.nativeElement.textContent).not.toContain('Aktuell sind keine Veranstaltungen verfügbar.');
    expect(fixture.nativeElement.textContent).toContain('Filter zurücksetzen');
  });

  it('setzt bei Klick auf "Filter zurücksetzen" im gefilterten Leerzustand beide Filter zurück', () => {
    const getVeranstaltungen = vi.fn().mockReturnValue(of([]));
    const fixture = createFixture({ getVeranstaltungen });
    fixture.detectChanges();

    setzeDatumsfeld(fixture, 'filter-bar-von', '2026-09-06');
    setzeDatumsfeld(fixture, 'filter-bar-bis', '2026-09-11');

    const zuruecksetzenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.programmuebersicht__filter-zuruecksetzen'
    );
    zuruecksetzenButton.click();
    fixture.detectChanges();

    expect(getVeranstaltungen).toHaveBeenCalledWith(null, null, null);
  });
});
