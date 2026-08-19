import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { VeranstaltungListItem } from '../../core/models/veranstaltung.model';
import { VeranstaltungslisteComponent } from './veranstaltungsliste';

describe('VeranstaltungslisteComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeranstaltungslisteComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lädt beim Start Spielstätten und Veranstaltungen und zeigt sie an', () => {
    const fixture = TestBed.createComponent(VeranstaltungslisteComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/spielstaetten').flush([{ id: 'V1', name: 'Stadthalle' }]);

    const veranstaltungen: VeranstaltungListItem[] = [
      { id: 'E1', titel: 'Testkonzert', spielstaetteId: 'V1', spielstaetteName: 'Stadthalle', zeitpunkt: '2026-09-05T19:30:00' }
    ];
    httpMock.expectOne((req) => req.url === '/api/veranstaltungen').flush(veranstaltungen);
    fixture.detectChanges();

    const karten = fixture.nativeElement.querySelectorAll('.veranstaltungs-karte');
    expect(karten.length).toBe(1);
    expect(karten[0].textContent).toContain('Testkonzert');
  });

  it('schickt die Filterwerte als Query-Parameter mit', () => {
    const fixture = TestBed.createComponent(VeranstaltungslisteComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/spielstaetten').flush([]);
    httpMock.expectOne((req) => req.url === '/api/veranstaltungen').flush([]);

    const component = fixture.componentInstance;
    component['spielstaetteId'] = 'V1';
    component['suchen']();

    const request = httpMock.expectOne(
      (req) => req.url === '/api/veranstaltungen' && req.params.get('spielstaetteId') === 'V1'
    );
    request.flush([]);
  });

  it('zeigt eine Fehlermeldung, wenn das Laden fehlschlägt', () => {
    const fixture = TestBed.createComponent(VeranstaltungslisteComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/spielstaetten').flush([]);
    httpMock
      .expectOne((req) => req.url === '/api/veranstaltungen')
      .flush({ message: 'Serverfehler' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.fehler')?.textContent).toContain('Serverfehler');
  });
});
