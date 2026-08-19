import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { VeranstaltungDetail } from '../shared/models/veranstaltung-detail';
import { Veranstaltungsdetail } from './veranstaltungsdetail';

describe('Veranstaltungsdetail', () => {
  const veranstaltung: VeranstaltungDetail = {
    id: 'E1',
    titel: 'Kammerkonzert Frühling',
    beschreibung: 'Ein intimes Konzert mit klassischen und modernen Stücken für ein kleines Ensemble.',
    dauerMinuten: 90,
    altersfreigabe: 0,
    spielstaetteName: 'Stadthalle Nordpark',
    raumName: 'Kleiner Saal',
    zeitpunkt: '2026-09-05T19:30:00+02:00',
    preiskategorien: []
  };

  function createFixture(serviceStub: Pick<VeranstaltungenService, 'getVeranstaltung'>) {
    TestBed.configureTestingModule({
      imports: [Veranstaltungsdetail],
      providers: [
        provideRouter([]),
        { provide: VeranstaltungenService, useValue: serviceStub },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: 'E1' })) } }
      ]
    });
    return TestBed.createComponent(Veranstaltungsdetail);
  }

  it('zeigt Titel, Beschreibung, Dauer, Altersfreigabe, Spielstätte und Raum an', () => {
    const fixture = createFixture({ getVeranstaltung: () => of(veranstaltung) });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Kammerkonzert Frühling');
    expect(text).toContain('Ein intimes Konzert mit klassischen und modernen Stücken für ein kleines Ensemble.');
    expect(text).toContain('90 Minuten');
    expect(text).toContain('Ohne Altersbeschränkung');
    expect(text).toContain('Stadthalle Nordpark');
    expect(text).toContain('Kleiner Saal');
    expect(text).toContain('19:30');
  });

  it('zeigt einen Ladehinweis, solange die Veranstaltung noch lädt', () => {
    const nichtAufgeloest = new Subject<VeranstaltungDetail>();
    const fixture = createFixture({ getVeranstaltung: () => nichtAufgeloest.asObservable() });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Veranstaltung wird geladen');
    expect(fixture.nativeElement.querySelector('.veranstaltungsdetail')).toBeNull();
  });

  it('rendert den CTA-Link mit dem exakten Pfad zur Sitzplatzauswahl', () => {
    const fixture = createFixture({ getVeranstaltung: () => of(veranstaltung) });
    fixture.detectChanges();

    const cta: HTMLAnchorElement = fixture.nativeElement.querySelector('.veranstaltungsdetail__cta');
    expect(cta.getAttribute('href')).toBe('/veranstaltungen/E1/buchung');
  });

  it('zeigt bei fehlgeschlagenem Laden eine Fehlermeldung statt leerer Fläche', () => {
    const fixture = createFixture({ getVeranstaltung: () => throwError(() => new Error('Netzwerkfehler')) });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Veranstaltung konnte nicht geladen werden.');
  });

  it('rendert die Altersfreigabe als reinen Text ohne interaktives Element', () => {
    const fixture = createFixture({ getVeranstaltung: () => of({ ...veranstaltung, altersfreigabe: 16 }) });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ab 16 Jahren');
    const daten: HTMLElement = fixture.nativeElement.querySelector('.veranstaltungsdetail__daten');
    expect(daten.querySelector('input')).toBeNull();
    expect(daten.querySelector('button')).toBeNull();
  });
});
