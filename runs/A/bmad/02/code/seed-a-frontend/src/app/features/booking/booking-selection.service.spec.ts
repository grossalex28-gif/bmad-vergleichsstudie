import { TestBed } from '@angular/core/testing';

import { BookingSelectionService } from './booking-selection.service';

describe('BookingSelectionService', () => {
  function createService(): BookingSelectionService {
    TestBed.configureTestingModule({ providers: [BookingSelectionService] });
    return TestBed.inject(BookingSelectionService);
  }

  it('markiert einen Sitzplatz nach toggle() als ausgewählt', () => {
    const service = createService();
    service.toggle('A', 1);
    expect(service.isSelected('A', 1)).toBe(true);
    expect(service.selectedSeats()).toEqual([{ rowLabel: 'A', columnNumber: 1, priceCategoryId: null }]);
  });

  it('hebt die Auswahl bei erneutem toggle() wieder auf', () => {
    const service = createService();
    service.toggle('A', 1);
    service.toggle('A', 1);
    expect(service.isSelected('A', 1)).toBe(false);
    expect(service.selectedSeats()).toEqual([]);
  });

  it('erlaubt mehrere, auch nicht zusammenhängende Sitzplätze gleichzeitig ausgewählt zu halten', () => {
    const service = createService();
    service.toggle('A', 1);
    service.toggle('C', 5);
    expect(service.selectedSeats()).toEqual([
      { rowLabel: 'A', columnNumber: 1, priceCategoryId: null },
      { rowLabel: 'C', columnNumber: 5, priceCategoryId: null }
    ]);
  });

  it('lässt die Auswahl der übrigen Sitzplätze unverändert, wenn ein anderer abgewählt wird', () => {
    const service = createService();
    service.toggle('A', 1);
    service.toggle('C', 5);
    service.toggle('A', 1);
    expect(service.selectedSeats()).toEqual([{ rowLabel: 'C', columnNumber: 5, priceCategoryId: null }]);
  });

  it('ordnet einem Sitzplatz eine Preiskategorie zu, ohne die Zuordnung anderer Sitzplätze zu ändern', () => {
    const service = createService();
    service.toggle('A', 1);
    service.toggle('C', 5);
    service.assignCategory('A', 1, 10);
    expect(service.selectedSeats()).toEqual([
      { rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 },
      { rowLabel: 'C', columnNumber: 5, priceCategoryId: null }
    ]);
  });

  it('entfernt die zugeordnete Preiskategorie, wenn der Sitzplatz abgewählt wird', () => {
    const service = createService();
    service.toggle('A', 1);
    service.assignCategory('A', 1, 10);
    service.toggle('A', 1);
    service.toggle('A', 1);
    expect(service.selectedSeats()).toEqual([{ rowLabel: 'A', columnNumber: 1, priceCategoryId: null }]);
  });

  it('allSelectedSeatsHaveCategory ist erst true, wenn jeder ausgewählte Sitzplatz eine Kategorie hat', () => {
    const service = createService();
    expect(service.allSelectedSeatsHaveCategory()).toBe(false);
    service.toggle('A', 1);
    service.toggle('C', 5);
    expect(service.allSelectedSeatsHaveCategory()).toBe(false);
    service.assignCategory('A', 1, 10);
    expect(service.allSelectedSeatsHaveCategory()).toBe(false);
    service.assignCategory('C', 5, 10);
    expect(service.allSelectedSeatsHaveCategory()).toBe(true);
  });

  it('totalPrice ist 0 ohne Auswahl', () => {
    const service = createService();
    expect(service.totalPrice()).toBe(0);
  });

  it('totalPrice zählt nur Sitzplätze mit zugeordneter Kategorie, summiert korrekt über mehrere Positionen', () => {
    const service = createService();
    service.setPriceCategories([
      { id: 10, name: 'Kategorie A', preis: 25 },
      { id: 20, name: 'Kategorie B', preis: 45 }
    ]);
    service.toggle('A', 1);
    service.toggle('C', 5);
    expect(service.totalPrice()).toBe(0); // beide ohne Kategorie
    service.assignCategory('A', 1, 10);
    expect(service.totalPrice()).toBe(25);
    service.assignCategory('C', 5, 20);
    expect(service.totalPrice()).toBe(70);
  });

  it('totalPrice aktualisiert sich, wenn eine Kategorie-Zuordnung geändert wird', () => {
    const service = createService();
    service.setPriceCategories([
      { id: 10, name: 'Kategorie A', preis: 25 },
      { id: 20, name: 'Kategorie B', preis: 45 }
    ]);
    service.toggle('A', 1);
    service.assignCategory('A', 1, 10);
    expect(service.totalPrice()).toBe(25);
    service.assignCategory('A', 1, 20);
    expect(service.totalPrice()).toBe(45);
  });

  it('totalPrice sinkt, wenn ein Sitzplatz mit zugeordneter Kategorie abgewählt wird', () => {
    const service = createService();
    service.setPriceCategories([{ id: 10, name: 'Kategorie A', preis: 25 }]);
    service.toggle('A', 1);
    service.assignCategory('A', 1, 10);
    service.toggle('A', 1);
    expect(service.totalPrice()).toBe(0);
  });

  it('removeSeats entfernt genau die angegebenen Sitzplätze und lässt die übrigen inkl. Kategorie unverändert', () => {
    const service = createService();
    service.toggle('A', 1);
    service.toggle('B', 2);
    service.assignCategory('A', 1, 10);
    service.assignCategory('B', 2, 20);

    service.removeSeats([{ rowLabel: 'A', columnNumber: 1 }]);

    expect(service.selectedSeats()).toEqual([{ rowLabel: 'B', columnNumber: 2, priceCategoryId: 20 }]);
  });

  it('removeSeats mit einer leeren Liste ändert die Auswahl nicht', () => {
    const service = createService();
    service.toggle('A', 1);
    service.assignCategory('A', 1, 10);

    service.removeSeats([]);

    expect(service.selectedSeats()).toEqual([{ rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 }]);
  });

  it('removeSeats mit einem nicht ausgewählten Sitzplatz ist ein No-Op für die vorhandene Auswahl', () => {
    const service = createService();
    service.toggle('A', 1);
    service.assignCategory('A', 1, 10);

    service.removeSeats([{ rowLabel: 'C', columnNumber: 9 }]);

    expect(service.selectedSeats()).toEqual([{ rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 }]);
  });
});
