import { TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';

import { PriceSummary } from './price-summary';
import { BookingSelectionService } from '../booking-selection.service';
import { PriceCategory } from '../../../core/api/event-detail';

registerLocaleData(localeDe);

describe('PriceSummary', () => {
  const categories: PriceCategory[] = [
    { id: 10, name: 'Kategorie A', preis: 25 },
    { id: 20, name: 'Kategorie B', preis: 45 }
  ];

  function createFixture() {
    TestBed.configureTestingModule({
      imports: [PriceSummary],
      providers: [BookingSelectionService, { provide: LOCALE_ID, useValue: 'de-DE' }]
    });
    const fixture = TestBed.createComponent(PriceSummary);
    fixture.componentRef.setInput('priceCategories', categories);
    const selection = TestBed.inject(BookingSelectionService);
    selection.setPriceCategories(categories);
    return { fixture, selection };
  }

  it('zeigt "0,00 €" ohne Auswahl statt einer leeren Fläche', () => {
    const { fixture } = createFixture();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('0,00 €');
  });

  it('aktualisiert den Gesamtpreis unmittelbar, wenn eine Preiskategorie zugeordnet wird', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('0,00 €');

    selection.assignCategory('A', 1, 10);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('25,00 €');
  });

  it('summiert mehrere Buchungspositionen zum Gesamtpreis', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.assignCategory('A', 1, 10);
    selection.toggle('C', 5);
    selection.assignCategory('C', 5, 20);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('70,00 €');
  });
});
