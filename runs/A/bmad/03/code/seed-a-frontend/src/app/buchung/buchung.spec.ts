import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { BuchungenService } from '../shared/api/buchungen.service';
import { VeranstaltungenService } from '../shared/api/veranstaltungen.service';
import { Buchung as BuchungDto } from '../shared/models/buchung';
import { Preiskategorie } from '../shared/models/preiskategorie';
import { Sitzplan, SitzplanPosition, SitzplanReihe } from '../shared/models/sitzplan';
import { VeranstaltungDetail } from '../shared/models/veranstaltung-detail';
import { Buchung } from './buchung';

registerLocaleData(localeDe);

describe('Buchung', () => {
  function erzeugeBuchung(overrides: Partial<BuchungDto> = {}): BuchungDto {
    return {
      referenz: 'ABCD1234',
      veranstaltungId: 'E1',
      name: 'Max Mustermann',
      email: 'max@example.com',
      status: 'Aktiv',
      gesamtpreis: 32,
      positionen: [
        { sitzplatzCode: 'A1', preiskategorieId: 'E1-A', preiskategorieName: 'Kategorie A', preisSnapshot: 32 },
      ],
      ...overrides,
    };
  }

  function erzeugeVeranstaltungDetail(
    preiskategorien: Preiskategorie[] = [
      { id: 'E1-A', name: 'Kategorie A', preis: 32 },
      { id: 'E1-B', name: 'Kategorie B', preis: 22 },
    ],
  ): VeranstaltungDetail {
    return {
      id: 'E1',
      titel: 'Kammerkonzert Frühling',
      beschreibung: 'Ein intimes Konzert.',
      dauerMinuten: 90,
      altersfreigabe: 0,
      spielstaetteName: 'Stadthalle Nordpark',
      raumName: 'Kleiner Saal',
      zeitpunkt: '2026-09-05T19:30:00+02:00',
      preiskategorien,
    };
  }

  function positionenFuerReihe(belegteCodes: string[], reihe: string): SitzplanPosition[] {
    const positionen: SitzplanPosition[] = [];
    for (let spalte = 1; spalte <= 10; spalte++) {
      if (spalte === 6) {
        positionen.push({ spalte, typ: 'Gang', code: null, status: null });
      } else {
        const code = `${reihe}${spalte}`;
        positionen.push({
          spalte,
          typ: 'Sitzplatz',
          code,
          status: belegteCodes.includes(code) ? 'Belegt' : 'Frei',
        });
      }
    }
    return positionen;
  }

  function erzeugeR2Sitzplan(): Sitzplan {
    const belegteCodes = ['B3', 'B4', 'C7'];
    const reihen: SitzplanReihe[] = ['A', 'B', 'C', 'D', 'E', 'F'].map((reihe) => ({
      reihe,
      positionen: positionenFuerReihe(belegteCodes, reihe),
    }));
    return { veranstaltungId: 'E1', raumId: 'R2', raumName: 'Kleiner Saal', reihen };
  }

  function seatDiv(fixture: { nativeElement: HTMLElement }, code: string): HTMLElement {
    return fixture.nativeElement.querySelector(`.seat-tile[title="${code}"]`)!;
  }

  function createFixture(
    serviceStub: Pick<VeranstaltungenService, 'getSitzplan' | 'getVeranstaltung'>,
    buchungenServiceStub: Pick<BuchungenService, 'buchungAnlegen'> = {
      buchungAnlegen: () => of(erzeugeBuchung()),
    },
  ) {
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        { provide: VeranstaltungenService, useValue: serviceStub },
        { provide: BuchungenService, useValue: buchungenServiceStub },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: 'E1' })) } },
      ],
    });
    return TestBed.createComponent(Buchung);
  }

  it('rendert an einer Gang-Position keine app-seat-tile, sondern eine sitzplan__luecke', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const ersteReihe: HTMLElement = fixture.nativeElement.querySelector('.sitzplan__reihe');
    expect(ersteReihe.querySelector('.sitzplan__luecke')).not.toBeNull();
    expect(ersteReihe.querySelectorAll('app-seat-tile').length).toBe(9);
  });

  it('rendert für eine belegte Sitzplatz-Position eine app-seat-tile mit Status belegt', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const kacheln: HTMLElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('app-seat-tile'),
    );
    const belegteKachel = kacheln.find((k) =>
      k.querySelector('.seat-tile')?.getAttribute('aria-label')?.includes('belegt'),
    );
    expect(belegteKachel).toBeDefined();
  });

  it('rendert für die R2-Geometrie 54 app-seat-tile-Elemente und 6 sitzplan__luecke-Elemente', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-seat-tile').length).toBe(54);
    expect(fixture.nativeElement.querySelectorAll('.sitzplan__luecke').length).toBe(6);
  });

  it('zeigt vor Auflösung den Skeleton-Container an, keine app-seat-tile', () => {
    const nichtAufgeloest = new Subject<Sitzplan>();
    const fixture = createFixture({
      getSitzplan: () => nichtAufgeloest.asObservable(),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    expect(container.getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('app-seat-tile').length).toBe(0);
  });

  it('zeigt bei fehlgeschlagenem Laden eine Fehlermeldung statt leerer Fläche', () => {
    const fixture = createFixture({
      getSitzplan: () => throwError(() => new Error('Netzwerkfehler')),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Sitzplan konnte nicht geladen werden.');
  });

  it('laedt den Sitzplan bei jeder neuen Routenaktivierung frisch statt aus einem vorherigen Aufruf zwischenzuspeichern', () => {
    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
    let aufrufe = 0;
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => {
              aufrufe++;
              return of(erzeugeR2Sitzplan());
            },
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E1' }));
    fixture.detectChanges();
    expect(aufrufe).toBe(1);

    paramMap$.next(convertToParamMap({ id: 'E2' }));
    fixture.detectChanges();
    expect(aufrufe).toBe(2);
  });

  it('rendert den sitzplan-Container mit horizontalem Scrollen und einer Reihenbeschriftung pro Zeile', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.sitzplan')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.sitzplan__reihen-label').length).toBe(6);
  });

  it('schaltet die Auswahl eines freien Platzes per Klick um und beim zweiten Klick wieder zurück', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    expect(seatDiv(fixture, 'A1').classList.contains('seat-tile--ausgewaehlt')).toBe(true);

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    expect(seatDiv(fixture, 'A1').classList.contains('seat-tile--ausgewaehlt')).toBe(false);
  });

  it('ändert bei Klick auf einen belegten Platz nichts am Auswahlzustand', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'B3').click();
    fixture.detectChanges();

    expect(seatDiv(fixture, 'B3').classList.contains('seat-tile--ausgewaehlt')).toBe(false);
  });

  it('aktualisiert den Roving-Tabindex-Fokus beim Klick auf eine nicht fokussierte Kachel, sodass eine nachfolgende Pfeiltastennavigation von dort aus fortsetzt', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A1').getAttribute('tabindex')).toBe('0');

    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A3').getAttribute('tabindex')).toBe('0');
    expect(seatDiv(fixture, 'A1').getAttribute('tabindex')).toBe('-1');

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A4').getAttribute('tabindex')).toBe('0');
  });

  it('bewegt den Fokus bei ArrowRight zur nächsten Sitzplatz-Position und überspringt dabei die Gang-Spalte', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    fixture.componentInstance.fokussiertePosition.set({ reihe: 'A', spalte: 5 });
    fixture.detectChanges();

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A7').getAttribute('tabindex')).toBe('0');
    expect(seatDiv(fixture, 'A5').getAttribute('tabindex')).toBe('-1');
  });

  it('hat zu jedem Zeitpunkt genau eine app-seat-tile mit tabindex="0"', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const kacheln: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.seat-tile'));
    expect(kacheln.filter((k) => k.getAttribute('tabindex') === '0').length).toBe(1);

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    fixture.detectChanges();

    const kachelnNachBewegung: HTMLElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.seat-tile'),
    );
    expect(kachelnNachBewegung.filter((k) => k.getAttribute('tabindex') === '0').length).toBe(1);
  });

  it('schaltet mit Leertaste die Auswahl der fokussierten freien Position um', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: ' ' }));
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A1').classList.contains('seat-tile--ausgewaehlt')).toBe(true);
  });

  it('bewegt den Fokus bei ArrowDown/ArrowUp eine Reihe weiter bzw. zurück bei gleichem Spalten-Index', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    fixture.detectChanges();
    expect(seatDiv(fixture, 'B1').getAttribute('tabindex')).toBe('0');

    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    fixture.detectChanges();
    expect(seatDiv(fixture, 'A1').getAttribute('tabindex')).toBe('0');
  });

  it('persistiert die Auswahl unter keinen Umständen und eine neue Komponenteninstanz startet mit leerer Auswahl', () => {
    const setItemSpy = vi.spyOn(Storage.prototype, 'setItem');
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    const container: HTMLElement = fixture.nativeElement.querySelector('.sitzplan');
    container.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(setItemSpy).not.toHaveBeenCalled();

    TestBed.resetTestingModule();
    const zweiteFixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    zweiteFixture.detectChanges();
    expect(zweiteFixture.componentInstance.ausgewaehlteCodes().size).toBe(0);

    setItemSpy.mockRestore();
  });

  it('setzt die Auswahl bei einer Routenreaktivierung mit neuer id zurück, statt sie aus der vorherigen Veranstaltung zu übernehmen', () => {
    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => of(erzeugeR2Sitzplan()),
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E1' }));
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    expect(fixture.componentInstance.ausgewaehlteCodes().size).toBe(1);

    paramMap$.next(convertToParamMap({ id: 'E2' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.ausgewaehlteCodes().size).toBe(0);
  });

  it('rendert nach Auswahl eines freien Platzes eine Auswahlliste mit einem Select-Feld, das die Preiskategorien der Veranstaltung anbietet', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const auswahl: HTMLElement = fixture.nativeElement.querySelector('.sitzplatz-auswahl');
    expect(auswahl).not.toBeNull();
    const select: HTMLSelectElement = auswahl.querySelector('select')!;
    expect(select.querySelectorAll('option').length).toBe(3);
  });

  it('deaktiviert den Weiter-Button ohne Kategoriezuordnung und aktiviert ihn nach Auswahl einer Kategorie', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const weiterButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__weiter',
    );
    expect(weiterButton.disabled).toBe(true);

    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      '.sitzplatz-auswahl__select',
    );
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(weiterButton.disabled).toBe(false);
  });

  it('hält den Weiter-Button deaktiviert, solange von mehreren ausgewählten Plätzen nur ein Teil eine Preiskategorie hat', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    const weiterButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__weiter',
    );
    const selectA1: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A1');
    selectA1.value = 'E1-A';
    selectA1.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(weiterButton.disabled).toBe(true);
  });

  it('speichert für zwei ausgewählte Sitzplätze unterschiedliche Preiskategorien getrennt nach Code', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    const selects: HTMLSelectElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.sitzplatz-auswahl__select'),
    );
    const selectA1 = selects.find((s) => s.id === 'kategorie-A1')!;
    const selectA3 = selects.find((s) => s.id === 'kategorie-A3')!;

    selectA1.value = 'E1-A';
    selectA1.dispatchEvent(new Event('change'));
    selectA3.value = 'E1-B';
    selectA3.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const zuordnung = fixture.componentInstance.kategorieZuordnung();
    expect(zuordnung.get('A1')).toBe('E1-A');
    expect(zuordnung.get('A3')).toBe('E1-B');
  });

  it('entfernt die zugeordnete Preiskategorie, wenn der Sitzplatz wieder abgewählt wird', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      '.sitzplatz-auswahl__select',
    );
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(fixture.componentInstance.kategorieZuordnung().has('A1')).toBe(true);

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.kategorieZuordnung().has('A1')).toBe(false);
  });

  it('setzt die Kategoriezuordnung bei einer Routenreaktivierung mit neuer id zurück', () => {
    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => of(erzeugeR2Sitzplan()),
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E1' }));
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      '.sitzplatz-auswahl__select',
    );
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(fixture.componentInstance.kategorieZuordnung().size).toBe(1);

    paramMap$.next(convertToParamMap({ id: 'E2' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.kategorieZuordnung().size).toBe(0);
  });

  it('berechnet den Gesamtpreis aus den zugeordneten Preiskategorien der ausgewählten Plätze', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    const selectA1: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A1');
    selectA1.value = 'E1-A';
    selectA1.dispatchEvent(new Event('change'));
    const selectA3: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A3');
    selectA3.value = 'E1-B';
    selectA3.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.gesamtpreis()).toBe(54);

    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.gesamtpreis()).toBe(32);
  });

  it('liefert einen Gesamtpreis von 0 statt NaN, solange ein ausgewählter Platz noch keine Preiskategorie hat', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.gesamtpreis()).toBe(0);
  });

  it('zeigt den formatierten Gesamtpreis in der price-summary-bar an', () => {
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'de-DE' },
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => of(erzeugeR2Sitzplan()),
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: 'E1' })) } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      '.sitzplatz-auswahl__select',
    );
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const preisElement: HTMLElement = fixture.nativeElement.querySelector('.price-summary-bar__preis');
    expect(preisElement.textContent).toContain('32,00');
  });

  it('kündigt Preisänderungen per aria-live="polite" an', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const preisElement: HTMLElement = fixture.nativeElement.querySelector('.price-summary-bar__preis');
    expect(preisElement.getAttribute('aria-live')).toBe('polite');
  });

  it('schaltet die Auswahl-Details über den details-toggle-Button auf und wieder zu', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__details-toggle',
    );
    toggle.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(true);
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(false);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });

  it('zeigt die Auswahlliste standardmäßig eingeklappt (Klasse sitzplatz-auswahl--eingeklappt gesetzt)', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const auswahl: HTMLElement = fixture.nativeElement.querySelector('.sitzplatz-auswahl');
    expect(auswahl.classList.contains('sitzplatz-auswahl--eingeklappt')).toBe(true);
  });

  it('setzt detailsAusgeklappt bei einer Routenreaktivierung mit neuer id zurück', () => {
    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => of(erzeugeR2Sitzplan()),
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E1' }));
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();
    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__details-toggle',
    );
    toggle.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(true);

    paramMap$.next(convertToParamMap({ id: 'E2' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(false);
  });

  it('zeigt die Anzahl gewählter Plätze in der price-summary-bar an und wechselt zwischen Singular und Plural', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const anzahlElement: HTMLElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__anzahl',
    );
    expect(anzahlElement.textContent).toContain('1 Platz');
    expect(anzahlElement.textContent).not.toContain('Plätze');

    seatDiv(fixture, 'A3').click();
    fixture.detectChanges();

    expect(anzahlElement.textContent).toContain('2 Plätze');
  });

  it('setzt detailsAusgeklappt zurück, sobald alle Plätze abgewählt werden', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__details-toggle',
    );
    toggle.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(true);

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(false);

    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.detailsAusgeklappt()).toBe(false);
  });

  function geheZuSchritt2(fixture: ReturnType<typeof createFixture>): void {
    seatDiv(fixture, 'A1').click();
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      '.sitzplatz-auswahl__select',
    );
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const weiterButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__weiter',
    );
    weiterButton.click();
    fixture.detectChanges();
  }

  it('wechselt bei Klick auf Weiter zu Schritt 2 mit Kontaktformular statt Sitzplan/Auswahlliste', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    expect(fixture.componentInstance.schritt()).toBe(2);
    expect(fixture.nativeElement.querySelector('.kontaktformular')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.sitzplan')).toBeNull();
    expect(fixture.nativeElement.querySelector('.sitzplatz-auswahl')).toBeNull();
  });

  it('verknüpft in Schritt 2 die Felder Name und E-Mail mit sichtbaren Labels über das for-Attribut', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    const nameLabel: HTMLLabelElement = fixture.nativeElement.querySelector(
      'label[for="kontakt-name"]',
    );
    const emailLabel: HTMLLabelElement = fixture.nativeElement.querySelector(
      'label[for="kontakt-email"]',
    );
    expect(nameLabel).not.toBeNull();
    expect(emailLabel).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#kontakt-name')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#kontakt-email')).not.toBeNull();
  });

  it('hält Buchung-abschließen deaktiviert, solange Name oder E-Mail fehlt', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.value = 'a@b.de';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(abschliessenButton.disabled).toBe(true);

    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-name');
    nameInput.value = 'Max Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    emailInput.value = '';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(abschliessenButton.disabled).toBe(true);
  });

  it('zeigt einen Inline-Fehler für ein ungültiges E-Mail-Format erst nach Verlassen des Felds', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.value = 'ungueltig';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.kontaktformular__fehler')).toBeNull();

    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const fehler = fixture.nativeElement.querySelector('.kontaktformular__fehler');
    expect(fehler).not.toBeNull();
    expect(emailInput.getAttribute('aria-describedby')).toBe('kontakt-email-fehler');
  });

  it('zeigt bei leerem E-Mail-Feld nach Verlassen keinen Formatfehler', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.kontaktformular__fehler')).toBeNull();
  });

  it('aktiviert Buchung abschließen, sobald Name und eine gültige E-Mail-Adresse eingegeben sind', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);

    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-name');
    nameInput.value = 'Max Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.value = 'a@b.de';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    expect(abschliessenButton.disabled).toBe(false);
  });

  it('erhält bei Rückkehr zu Schritt 1 die bisherige Sitzplatzauswahl und Preiskategorien', () => {
    const fixture = createFixture({
      getSitzplan: () => of(erzeugeR2Sitzplan()),
      getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
    });
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    const codesVorRueckkehr = fixture.componentInstance.ausgewaehlteCodes().size;
    const zuordnungVorRueckkehr = fixture.componentInstance.kategorieZuordnung().size;

    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-name');
    nameInput.value = 'Max Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const zurueckButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__zurueck',
    );
    zurueckButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.schritt()).toBe(1);
    expect(fixture.nativeElement.querySelector('.sitzplan')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.sitzplatz-auswahl')).not.toBeNull();
    expect(fixture.componentInstance.ausgewaehlteCodes().size).toBe(codesVorRueckkehr);
    expect(fixture.componentInstance.kategorieZuordnung().size).toBe(zuordnungVorRueckkehr);
  });

  it('setzt Schritt und Kontaktdaten bei einer Routenreaktivierung mit neuer id zurück', () => {
    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
    TestBed.configureTestingModule({
      imports: [Buchung],
      providers: [
        provideRouter([]),
        {
          provide: VeranstaltungenService,
          useValue: {
            getSitzplan: () => of(erzeugeR2Sitzplan()),
            getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
          },
        },
        { provide: BuchungenService, useValue: { buchungAnlegen: () => of(erzeugeBuchung()) } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap$.asObservable() } },
      ],
    });
    const fixture = TestBed.createComponent(Buchung);
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E1' }));
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-name');
    nameInput.value = 'Max Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.value = 'a@b.de';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    paramMap$.next(convertToParamMap({ id: 'E2' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.schritt()).toBe(1);
    expect(fixture.componentInstance.kontaktName()).toBe('');
    expect(fixture.componentInstance.kontaktEmail()).toBe('');
  });

  function fuelleKontaktdaten(fixture: ReturnType<typeof createFixture>): void {
    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-name');
    nameInput.value = 'Max Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    const emailInput: HTMLInputElement = fixture.nativeElement.querySelector('#kontakt-email');
    emailInput.value = 'max@example.com';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  it('zeigt beim Abschließen einen Ladezustand, deaktiviert den Button und verhindert eine doppelte Einreichung', () => {
    const nichtAufgeloest = new Subject<BuchungDto>();
    const buchungAnlegen = vi.fn().mockReturnValue(nichtAufgeloest.asObservable());
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      { buchungAnlegen },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    abschliessenButton.click();
    fixture.detectChanges();

    expect(abschliessenButton.disabled).toBe(true);
    expect(abschliessenButton.textContent).toContain('Wird gebucht…');

    abschliessenButton.click();
    fixture.detectChanges();

    expect(buchungAnlegen).toHaveBeenCalledTimes(1);
  });

  it('zeigt nach erfolgreichem Abschluss die Buchungsbestätigung mit Referenz, Positionen und Gesamtpreis', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          of(
            erzeugeBuchung({
              referenz: 'XYZ98765',
              positionen: [
                { sitzplatzCode: 'A1', preiskategorieId: 'E1-A', preiskategorieName: 'Kategorie A', preisSnapshot: 32 },
              ],
              gesamtpreis: 54,
            }),
          ),
      },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    abschliessenButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.buchungsbestaetigung__titel').textContent).toContain(
      'Buchung bestätigt',
    );
    expect(fixture.nativeElement.querySelector('app-buchungsreferenz').textContent).toContain('XYZ98765');
    const position: HTMLElement = fixture.nativeElement.querySelector('.buchungsbestaetigung__position');
    expect(position.textContent).toContain('A1');
    expect(position.textContent).toContain('Kategorie A');
    expect(fixture.nativeElement.querySelector('.buchungsbestaetigung__gesamtpreis').textContent).toContain('54.00');
    expect(fixture.nativeElement.querySelector('.price-summary-bar')).toBeNull();
    expect(fixture.nativeElement.querySelector('.kontaktformular')).toBeNull();
  });

  it('zeigt bei einem Sitzplatzkonflikt für einen einzelnen Platz ein Konfliktbanner, hebt dessen Auswahl auf und lädt den Sitzplan neu', () => {
    let getSitzplanAufrufe = 0;
    const fixture = createFixture(
      {
        getSitzplan: () => {
          getSitzplanAufrufe++;
          return of(erzeugeR2Sitzplan());
        },
        getVeranstaltung: () => of(erzeugeVeranstaltungDetail()),
      },
      {
        buchungAnlegen: () =>
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: { type: 'sitzplatz_belegt', betroffeneSitzplaetze: ['A1'] },
              }),
          ),
      },
    );
    fixture.detectChanges();
    expect(getSitzplanAufrufe).toBe(1);

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    abschliessenButton.click();
    fixture.detectChanges();

    const banner: HTMLElement = fixture.nativeElement.querySelector('.buchung__fehler');
    expect(banner.textContent).toContain('Sitzplatz A1 ist inzwischen belegt.');
    expect(banner.getAttribute('aria-live')).toBe('assertive');
    expect(fixture.componentInstance.schritt()).toBe(1);
    expect(seatDiv(fixture, 'A1').classList.contains('seat-tile--ausgewaehlt')).toBe(false);
    expect(getSitzplanAufrufe).toBe(2);
  });

  it('zeigt bei einem Sitzplatzkonflikt für mehrere Plätze die Pluralform der Konfliktmeldung', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: { type: 'sitzplatz_belegt', betroffeneSitzplaetze: ['A1', 'B3'] },
              }),
          ),
      },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);

    const abschliessenButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__abschliessen',
    );
    abschliessenButton.click();
    fixture.detectChanges();

    const banner: HTMLElement = fixture.nativeElement.querySelector('.buchung__fehler');
    expect(banner.textContent).toContain('Sitzplätze A1, B3 sind inzwischen belegt.');
  });

  it('behält bei einem Konflikt für nur einen von mehreren gewählten Plätzen die übrige gültige Auswahl bei', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: { type: 'sitzplatz_belegt', betroffeneSitzplaetze: ['A1'] },
              }),
          ),
      },
    );
    fixture.detectChanges();

    seatDiv(fixture, 'A1').click();
    seatDiv(fixture, 'A2').click();
    fixture.detectChanges();
    const selectA1: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A1');
    selectA1.value = 'E1-A';
    selectA1.dispatchEvent(new Event('change'));
    const selectA2: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A2');
    selectA2.value = 'E1-B';
    selectA2.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.price-summary-bar__weiter').click();
    fixture.detectChanges();
    fuelleKontaktdaten(fixture);

    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A2').classList.contains('seat-tile--ausgewaehlt')).toBe(true);
    expect(fixture.componentInstance.kategorieZuordnung().get('A2')).toBe('E1-B');
    const preisElement: HTMLElement = fixture.nativeElement.querySelector('.price-summary-bar__preis');
    expect(preisElement.textContent).toContain('22.00');
  });

  it('kehrt nach einem Konflikt, der die gesamte verbleibende Auswahl kategorisiert lässt, mit aktivem Weiter-Button zu Schritt 1 zurück', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: { type: 'sitzplatz_belegt', betroffeneSitzplaetze: ['A1', 'A2'] },
              }),
          ),
      },
    );
    fixture.detectChanges();

    for (const code of ['A1', 'A2', 'A3']) {
      seatDiv(fixture, code).click();
    }
    fixture.detectChanges();
    for (const code of ['A1', 'A2', 'A3']) {
      const select: HTMLSelectElement = fixture.nativeElement.querySelector(`#kategorie-${code}`);
      select.value = 'E1-A';
      select.dispatchEvent(new Event('change'));
    }
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.price-summary-bar__weiter').click();
    fixture.detectChanges();
    fuelleKontaktdaten(fixture);

    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.schritt()).toBe(1);
    expect(fixture.componentInstance.ausgewaehlteCodes().size).toBe(1);
    expect(seatDiv(fixture, 'A3').classList.contains('seat-tile--ausgewaehlt')).toBe(true);
    const weiterButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.price-summary-bar__weiter',
    );
    expect(weiterButton.disabled).toBe(false);
  });

  it('verwirft bei einem anderen technischen Fehler die Auswahl nicht und zeigt eine verständliche Fehlermeldung', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          throwError(() => new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') })),
      },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);

    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();

    const fehler: HTMLElement = fixture.nativeElement.querySelector('.buchung__fehler');
    expect(fehler.textContent).toContain(
      'Die Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.',
    );
    expect(fixture.componentInstance.schritt()).toBe(2);

    fixture.nativeElement.querySelector('.price-summary-bar__zurueck').click();
    fixture.detectChanges();

    expect(seatDiv(fixture, 'A1').classList.contains('seat-tile--ausgewaehlt')).toBe(true);
  });

  it('räumt das Konfliktbanner auf, wenn ein erneuter Abschlussversuch mit einem anderen technischen Fehler fehlschlägt', () => {
    let versuch = 0;
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () => {
          versuch++;
          if (versuch === 1) {
            return throwError(
              () =>
                new HttpErrorResponse({
                  status: 409,
                  error: { type: 'sitzplatz_belegt', betroffeneSitzplaetze: ['A1'] },
                }),
            );
          }
          return throwError(() => new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }));
        },
      },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);
    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.buchung__fehler').textContent).toContain(
      'Sitzplatz A1 ist inzwischen belegt.',
    );

    seatDiv(fixture, 'A2').click();
    fixture.detectChanges();
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#kategorie-A2');
    select.value = 'E1-A';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.price-summary-bar__weiter').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();

    const fehlermeldungen = fixture.nativeElement.querySelectorAll('.buchung__fehler');
    expect(fehlermeldungen.length).toBe(1);
    expect(fehlermeldungen[0].textContent).toContain(
      'Die Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.',
    );
  });

  it('zeigt beim erneuten Wechsel zu Schritt 2 keine Fehlermeldung eines vorherigen Versuchs mehr, bevor erneut abgeschickt wurde', () => {
    const fixture = createFixture(
      { getSitzplan: () => of(erzeugeR2Sitzplan()), getVeranstaltung: () => of(erzeugeVeranstaltungDetail()) },
      {
        buchungAnlegen: () =>
          throwError(() => new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') })),
      },
    );
    fixture.detectChanges();

    geheZuSchritt2(fixture);
    fuelleKontaktdaten(fixture);
    fixture.nativeElement.querySelector('.price-summary-bar__abschliessen').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.buchung__fehler')).not.toBeNull();

    fixture.nativeElement.querySelector('.price-summary-bar__zurueck').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.price-summary-bar__weiter').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.buchung__fehler')).toBeNull();
  });
});
