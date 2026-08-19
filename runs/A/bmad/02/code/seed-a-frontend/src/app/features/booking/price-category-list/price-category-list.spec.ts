import { TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';

import { PriceCategoryList } from './price-category-list';
import { BookingSelectionService } from '../booking-selection.service';
import { PriceCategory } from '../../../core/api/event-detail';

registerLocaleData(localeDe);

describe('PriceCategoryList', () => {
  const categories: PriceCategory[] = [
    { id: 10, name: 'Kategorie A', preis: 25 },
    { id: 20, name: 'Kategorie B', preis: 45 }
  ];

  function createFixture() {
    TestBed.configureTestingModule({
      imports: [PriceCategoryList],
      providers: [BookingSelectionService, { provide: LOCALE_ID, useValue: 'de-DE' }]
    });
    const fixture = TestBed.createComponent(PriceCategoryList);
    fixture.componentRef.setInput('priceCategories', categories);
    const selection = TestBed.inject(BookingSelectionService);
    return { fixture, selection };
  }

  it('zeigt einen Hinweistext ohne ausgewählte Sitzplätze, kein <select>', () => {
    const { fixture } = createFixture();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('select')).toBeNull();
    expect(root.textContent).toContain('Wählen Sie Sitzplätze');
  });

  it('zeigt für jeden ausgewählten Sitzplatz eine eigene Preiskategorie-Zeile mit allen Kategorien', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('B', 3);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Sitzplatz B3');
    const options = root.querySelectorAll('select option');
    expect(options.length).toBe(3); // "Bitte wählen" + 2 Kategorien
    expect(root.textContent).toContain('Kategorie A – 25,00 €');
  });

  it('ordnet einem Sitzplatz per Auswahl im <select> eine Preiskategorie zu, ohne andere zu verändern', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.toggle('C', 5);
    fixture.detectChanges();

    const selects = fixture.nativeElement.querySelectorAll('select');
    selects[0].value = '10';
    selects[0].dispatchEvent(new Event('change'));

    expect(selection.selectedSeats()).toEqual([
      { rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 },
      { rowLabel: 'C', columnNumber: 5, priceCategoryId: null }
    ]);
  });

  it('entfernt die Zeile eines Sitzplatzes, sobald er im Service abgewählt wird', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('select').length).toBe(1);

    selection.toggle('A', 1);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('select').length).toBe(0);
  });
});
